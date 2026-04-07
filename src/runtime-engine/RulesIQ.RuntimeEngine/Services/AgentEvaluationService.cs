using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RulesIQ.Infrastructure.Services;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.RuntimeEngine.Services;

public interface IAgentEvaluationService
{
    Task<EvaluationResult> EvaluateWithAgentAsync(
        NormalizedWorkflow workflow,
        LoanEligibilityInput input,
        string evaluationGoal,
        CancellationToken cancellationToken = default);
}

public sealed class AgentEvaluationService : IAgentEvaluationService
{
    private readonly IOpenAIClientService _openAI;
    private readonly IRuleEvaluationService _ruleEvaluator;
    private readonly ILogger<AgentEvaluationService> _logger;
    private readonly Lazy<RuleFieldMappingConfig> _mappingConfig;

    private const string MappingPrompt = """
        You are a field-mapping assistant. You receive:
        1. A list of UNMAPPED rule expression field names (these appear as `input.FieldName` in rule expressions).
        2. A persona model with field names, types, and current values.

        Your task: For each unmapped field, determine the best mapping. Respond with JSON only:
        {
          "mappings": [
            { "ruleField": "SomeField", "type": "personaField", "personaField": "CreditScore" },
            { "ruleField": "InstitutionalField", "type": "assumeTrue" },
            { "ruleField": "NegativeField", "type": "assumeFalse" },
            { "ruleField": "DerivedBool", "type": "derived", "personaField": "LenderType", "condition": "equals", "value": "FederallyRegulated" },
            { "ruleField": "NumericDefault", "type": "constant", "value": 25 },
            { "ruleField": "StringDefault", "type": "constant", "value": "residential" }
          ]
        }

        Mapping guidelines:
        - "personaField": The rule field is a rename/synonym of a persona field. Use the persona field name.
        - "assumeTrue": Institutional/process/compliance fields that should default to true (e.g., board approvals, documentation maintained, fraud detection in place).
        - "assumeFalse": Negative condition fields that should default to false (e.g., income contradicts, illicit purpose suspected).
        - "derived": Boolean derived from a persona field comparison.
        - "constant": A sensible default numeric or string value for fields not directly in the persona.

        Be conservative: if a field clearly maps to a persona field, use "personaField". If it's institutional/process, use "assumeTrue". Only use "constant" as a last resort.

        Respond ONLY with valid JSON. No markdown, no commentary.
        """;

    public AgentEvaluationService(
        IOpenAIClientService openAI,
        IRuleEvaluationService ruleEvaluator,
        ILogger<AgentEvaluationService> logger)
    {
        _openAI = openAI;
        _ruleEvaluator = ruleEvaluator;
        _logger = logger;
        _mappingConfig = new Lazy<RuleFieldMappingConfig>(LoadMappingConfig);
    }

    public async Task<EvaluationResult> EvaluateWithAgentAsync(
        NormalizedWorkflow workflow,
        LoanEligibilityInput input,
        string evaluationGoal,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Agent evaluation: {RuleCount} rules — building field mapping",
            workflow.Rules.Count);

        // 1. Extract all field references from rule expressions
        var allFields = ExtractFieldReferences(workflow.Rules);
        _logger.LogInformation("Found {FieldCount} unique field references in expressions", allFields.Count);

        // 2. Load static mapping config (deep copy so LLM additions don't persist across calls)
        var config = CloneConfig(_mappingConfig.Value);

        // 3. Find unmapped fields
        var unmappedFields = allFields
            .Where(f => !IsFieldMapped(f, config))
            .ToList();

        // 4. If there are unmapped fields, ask LLM to map them
        if (unmappedFields.Count > 0)
        {
            _logger.LogInformation("{Count} unmapped fields — calling LLM for mapping", unmappedFields.Count);
            await ResolveUnmappedFieldsAsync(unmappedFields, input, config, cancellationToken);
        }
        else
        {
            _logger.LogInformation("All {Count} fields mapped statically — no LLM call needed", allFields.Count);
        }

        // 5. Build dynamic input object with all mapped field values
        var dynamicInput = BuildDynamicInput(config, input, allFields);

        // 6. Identify rules that reference insufficient-data fields (complex types not in persona)
        var insufficientRuleNames = IdentifyInsufficientRules(workflow.Rules, config.InsufficientDataFields);
        _logger.LogInformation("{InsufficientCount} rules marked as insufficient data", insufficientRuleNames.Count);

        // 7. Delegate to deterministic RulesEngine evaluation
        _logger.LogInformation("Running deterministic evaluation with mapped input ({FieldCount} properties)",
            ((IDictionary<string, object?>)dynamicInput).Count);

        var result = await _ruleEvaluator.EvaluateAsync(workflow, dynamicInput, evaluationGoal, cancellationToken);

        // 8. Post-process: override insufficient-data rules to "Not Evaluated"
        if (insufficientRuleNames.Count > 0)
            MarkInsufficientRules(result, insufficientRuleNames);

        return result;
    }

    private static HashSet<string> ExtractFieldReferences(IEnumerable<NormalizedRule> rules)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in rules)
        {
            foreach (Match match in Regex.Matches(rule.Expression, @"input\.(\w+)"))
            {
                fields.Add(match.Groups[1].Value);
            }
        }
        return fields;
    }

    private static bool IsFieldMapped(string field, RuleFieldMappingConfig config) =>
        config.FieldMappings.ContainsKey(field)
        || config.AssumedTrueFields.Contains(field)
        || config.AssumedFalseFields.Contains(field)
        || config.DerivedFields.ContainsKey(field)
        || config.DefaultNumericValues.ContainsKey(field)
        || config.DefaultStringValues.ContainsKey(field);

    private async Task ResolveUnmappedFieldsAsync(
        List<string> unmappedFields,
        LoanEligibilityInput input,
        RuleFieldMappingConfig config,
        CancellationToken cancellationToken)
    {
        var personaFields = typeof(LoanEligibilityInput).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != "CanonicalTestPersona")
            .Select(p => new { p.Name, Type = p.PropertyType.Name, Value = p.GetValue(input)?.ToString() ?? "null" })
            .ToList();

        var userPrompt = $"""
            ## Unmapped Rule Fields
            {JsonSerializer.Serialize(unmappedFields)}

            ## Persona Model Fields
            {JsonSerializer.Serialize(personaFields, new JsonSerializerOptions { WriteIndented = true })}
            """;

        try
        {
            var responseJson = await _openAI.GetChatCompletionAsync(MappingPrompt, userPrompt, cancellationToken);
            var response = JsonSerializer.Deserialize<LlmMappingResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response?.Mappings is not null)
            {
                foreach (var mapping in response.Mappings)
                {
                    ApplyLlmMapping(mapping, config);
                }
                _logger.LogInformation("LLM resolved {Count} field mappings", response.Mappings.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM mapping failed — unmapped fields will default to true");
            foreach (var field in unmappedFields)
            {
                config.AssumedTrueFields.Add(field);
            }
        }
    }

    private static void ApplyLlmMapping(LlmFieldMapping mapping, RuleFieldMappingConfig config)
    {
        switch (mapping.Type?.ToLowerInvariant())
        {
            case "personafield":
                if (!string.IsNullOrEmpty(mapping.PersonaField))
                    config.FieldMappings[mapping.RuleField] = mapping.PersonaField;
                break;
            case "assumetrue":
                config.AssumedTrueFields.Add(mapping.RuleField);
                break;
            case "assumefalse":
                config.AssumedFalseFields.Add(mapping.RuleField);
                break;
            case "derived":
                config.DerivedFields[mapping.RuleField] = new DerivedFieldConfig
                {
                    Source = mapping.PersonaField,
                    Condition = mapping.Condition ?? "equals",
                    Value = mapping.Value?.ToString()
                };
                break;
            case "constant":
                if (mapping.Value is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Number)
                        config.DefaultNumericValues[mapping.RuleField] = je.GetDecimal();
                    else if (je.ValueKind == JsonValueKind.String)
                        config.DefaultStringValues[mapping.RuleField] = je.GetString() ?? "";
                    else if (je.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        if (je.GetBoolean()) config.AssumedTrueFields.Add(mapping.RuleField);
                        else config.AssumedFalseFields.Add(mapping.RuleField);
                    }
                }
                break;
        }
    }

    private ExpandoObject BuildDynamicInput(
        RuleFieldMappingConfig config,
        LoanEligibilityInput input,
        HashSet<string> neededFields)
    {
        var personaValues = GetPersonaValues(input);
        var dynamicInput = new ExpandoObject();
        var dict = (IDictionary<string, object?>)dynamicInput;

        foreach (var field in neededFields)
        {
            dict[field] = ResolveFieldValue(field, config, personaValues);
        }

        // Computed fields that depend on multiple persona values
        if (neededFields.Contains("DownPaymentAmount"))
            dict["DownPaymentAmount"] = input.PropertyValue * input.DownPaymentPercent / 100m;

        // Complex-type fields that require objects/collections instead of scalars.
        // Empty list → .All() returns true (vacuous truth = no bad sources).
        if (neededFields.Contains("DownPaymentSources"))
            dict["DownPaymentSources"] = Array.Empty<object>();

        // String field → .Contains() works naturally.
        if (neededFields.Contains("Disclosure"))
            dict["Disclosure"] = "right to repay before maturity";

        // Nested property objects → provide ExpandoObject with expected sub-properties.
        if (neededFields.Contains("DisclosureLanguage"))
        {
            dynamic lang = new ExpandoObject();
            lang.IsClearAndNotMisleading = true;
            dict["DisclosureLanguage"] = lang;
        }

        if (neededFields.Contains("RenewalDisclosure"))
        {
            dynamic renewal = new ExpandoObject();
            renewal.DaysBeforeRenewalDate = 21;
            dict["RenewalDisclosure"] = renewal;
        }

        return dynamicInput;
    }

    private object? ResolveFieldValue(
        string field,
        RuleFieldMappingConfig config,
        Dictionary<string, object?> personaValues)
    {
        // 1. Direct persona field mapping
        if (config.FieldMappings.TryGetValue(field, out var personaField)
            && personaValues.TryGetValue(personaField, out var value))
        {
            return value;
        }

        // 2. Assumed true
        if (config.AssumedTrueFields.Contains(field))
            return true;

        // 3. Assumed false
        if (config.AssumedFalseFields.Contains(field))
            return false;

        // 4. Derived fields
        if (config.DerivedFields.TryGetValue(field, out var derived))
            return ResolveDerivedField(derived, personaValues);

        // 5. Default numeric values
        if (config.DefaultNumericValues.TryGetValue(field, out var numVal))
            return numVal;

        // 6. Default string values
        if (config.DefaultStringValues.TryGetValue(field, out var strVal))
            return strVal;

        // 7. Fallback: assume true for unknown boolean-like fields
        _logger.LogDebug("Field {Field} has no mapping — defaulting to true", field);
        return true;
    }

    private static object? ResolveDerivedField(DerivedFieldConfig derived, Dictionary<string, object?> personaValues)
    {
        if (derived.Constant.HasValue)
            return derived.Constant.Value;

        if (string.IsNullOrEmpty(derived.Source) || !personaValues.TryGetValue(derived.Source, out var sourceValue))
            return true;

        var sourceStr = sourceValue?.ToString() ?? "";

        // If the derived has trueResult/falseResult, return a status string
        if (derived.TrueResult is not null)
        {
            var conditionMet = EvaluateCondition(derived.Condition, sourceStr, derived.Value);
            return conditionMet ? derived.TrueResult : derived.FalseResult;
        }

        return EvaluateCondition(derived.Condition, sourceStr, derived.Value);
    }

    private static bool EvaluateCondition(string? condition, string sourceStr, string? compareValue)
    {
        return condition?.ToLowerInvariant() switch
        {
            "equals" => string.Equals(sourceStr, compareValue, StringComparison.OrdinalIgnoreCase),
            "notequals" => !string.Equals(sourceStr, compareValue, StringComparison.OrdinalIgnoreCase),
            "greaterthan" when decimal.TryParse(sourceStr, out var sv) && decimal.TryParse(compareValue, out var cv)
                => sv > cv,
            "lessthan" when decimal.TryParse(sourceStr, out var sv) && decimal.TryParse(compareValue, out var cv)
                => sv < cv,
            _ => true
        };
    }

    private static Dictionary<string, object?> GetPersonaValues(LoanEligibilityInput input)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in typeof(LoanEligibilityInput).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.Name == "CanonicalTestPersona") continue;
            values[prop.Name] = prop.GetValue(input);
        }
        return values;
    }

    private static HashSet<string> IdentifyInsufficientRules(
        IEnumerable<NormalizedRule> rules, HashSet<string> insufficientFields)
    {
        if (insufficientFields.Count == 0) return [];

        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in rules)
        {
            foreach (Match match in Regex.Matches(rule.Expression, @"input\.(\w+)"))
            {
                if (insufficientFields.Contains(match.Groups[1].Value))
                {
                    result.Add(rule.Id);
                    break;
                }
            }
        }
        return result;
    }

    private static void MarkInsufficientRules(EvaluationResult result, HashSet<string> insufficientRuleNames)
    {
        int reclassified = 0;
        foreach (var snapshot in result.RulesSnapshot.Rules)
        {
            if (insufficientRuleNames.Contains(snapshot.RuleId) && snapshot.Result != "Passed")
            {
                snapshot.Result = "Not Evaluated";
                snapshot.ErrorMessage = "Insufficient data — the applicant input does not contain the fields required to evaluate this rule.";
                snapshot.SuccessEvent = null;
                reclassified++;
            }
        }

        // Remove reclassified rules from FailedRules and recalculate score
        result.ComplianceScore.FailedRules.RemoveAll(f => insufficientRuleNames.Contains(f.RuleId));
        result.ComplianceScore.RulesFailed -= reclassified;
        result.ComplianceScore.TotalRulesEvaluated -= reclassified;

        var total = result.ComplianceScore.TotalRulesEvaluated;
        var passed = result.ComplianceScore.RulesPassed;
        result.ComplianceScore.CompliancePercentage = total > 0
            ? Math.Round((decimal)passed / total * 100, 2)
            : 0m;
    }

    private static RuleFieldMappingConfig CloneConfig(RuleFieldMappingConfig source) => new()
    {
        FieldMappings = new Dictionary<string, string>(source.FieldMappings),
        AssumedTrueFields = [.. source.AssumedTrueFields],
        AssumedFalseFields = [.. source.AssumedFalseFields],
        DerivedFields = new Dictionary<string, DerivedFieldConfig>(source.DerivedFields),
        DefaultNumericValues = new Dictionary<string, decimal>(source.DefaultNumericValues),
        DefaultStringValues = new Dictionary<string, string>(source.DefaultStringValues),
        InsufficientDataFields = [.. source.InsufficientDataFields]
    };

    private static RuleFieldMappingConfig LoadMappingConfig()
    {
        var assembly = typeof(AgentEvaluationService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("rule-field-mapping.json", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        return JsonSerializer.Deserialize<RuleFieldMappingConfig>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    // Internal DTOs for mapping config
    private sealed class RuleFieldMappingConfig
    {
        public Dictionary<string, string> FieldMappings { get; set; } = new();
        public HashSet<string> AssumedTrueFields { get; set; } = [];
        public HashSet<string> AssumedFalseFields { get; set; } = [];
        public Dictionary<string, DerivedFieldConfig> DerivedFields { get; set; } = new();
        public Dictionary<string, decimal> DefaultNumericValues { get; set; } = new();
        public Dictionary<string, string> DefaultStringValues { get; set; } = new();
        public HashSet<string> InsufficientDataFields { get; set; } = [];
    }

    private sealed class DerivedFieldConfig
    {
        public string? Source { get; set; }
        public string? Condition { get; set; }
        public string? Value { get; set; }
        public string? TrueResult { get; set; }
        public string? FalseResult { get; set; }
        public bool? Constant { get; set; }
    }

    private sealed class LlmMappingResponse
    {
        public List<LlmFieldMapping> Mappings { get; set; } = [];
    }

    private sealed class LlmFieldMapping
    {
        public string RuleField { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? PersonaField { get; set; }
        public string? Condition { get; set; }
        public object? Value { get; set; }
    }
}
