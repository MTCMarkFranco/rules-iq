using Microsoft.Extensions.Logging;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.RulesNormalizer.Services;

public interface INormalizationService
{
    NormalizedWorkflow Normalize(
        IReadOnlyList<ExtractionResult> candidateWorkflows,
        string? targetWorkflowName = null,
        string? rulesetVersion = null,
        string? sourceDocumentVersion = null);
}

public sealed class NormalizationService : INormalizationService
{
    private readonly ILogger<NormalizationService> _logger;

    public NormalizationService(ILogger<NormalizationService> logger)
    {
        _logger = logger;
    }

    public NormalizedWorkflow Normalize(
        IReadOnlyList<ExtractionResult> candidateWorkflows,
        string? targetWorkflowName = null,
        string? rulesetVersion = null,
        string? sourceDocumentVersion = null)
    {
        _logger.LogInformation("Normalizing {Count} candidate workflows", candidateWorkflows.Count);

        var allCandidateRules = candidateWorkflows
            .Where(cw => cw.HasRules && cw.Workflow is not null)
            .SelectMany(cw => cw.Workflow!.Rules)
            .ToList();

        var workflowName = targetWorkflowName
            ?? candidateWorkflows
                .FirstOrDefault(cw => cw.Workflow is not null)?.Workflow!.WorkflowName
            ?? "DefaultWorkflow";

        var (normalizedRules, mergeNotes) = DeduplicateAndNormalize(allCandidateRules, sourceDocumentVersion);

        var notes = BuildNormalizationNotes(allCandidateRules, normalizedRules, mergeNotes);

        _logger.LogInformation("Normalized to {Count} rules for workflow {Workflow}",
            normalizedRules.Count, workflowName);

        return new NormalizedWorkflow
        {
            WorkflowName = workflowName,
            RulesetVersion = rulesetVersion,
            SourceDocumentVersion = sourceDocumentVersion,
            RulesetPublishedTimestamp = DateTimeOffset.UtcNow,
            Rules = normalizedRules,
            NormalizationNotes = notes
        };
    }

    private (List<NormalizedRule> Rules, List<string> MergeNotes) DeduplicateAndNormalize(
        List<CandidateRule> rules, string? sourceDocumentVersion)
    {
        var grouped = rules
            .GroupBy(r => NormalizeExpression(r.Expression))
            .ToList();

        var normalizedRules = new List<NormalizedRule>();
        var mergeNotes = new List<string>();

        foreach (var group in grouped)
        {
            var rulesInGroup = group.ToList();
            var primary = rulesInGroup.OrderByDescending(r => r.RuleName.Length).First();

            // Aggregate metadata from ALL rules in the group (contract requirement)
            var sourceDocuments = rulesInGroup.Select(r => new SourceDocumentMetadata
            {
                SourceDocumentId = r.Metadata.SourceDocumentId,
                SourceUri = r.Metadata.SourceUri,
                SourceDocumentVersion = sourceDocumentVersion ?? r.Metadata.SourceDocumentVersion,
                PageNumber = r.Metadata.PageNumber,
                CharRange = r.Metadata.CharRange
            }).ToList();

            normalizedRules.Add(new NormalizedRule
            {
                RuleName = primary.RuleName,
                Expression = primary.Expression,
                SuccessEvent = primary.SuccessEvent,
                ErrorMessage = primary.ErrorMessage,
                RuleExpressionType = primary.RuleExpressionType ?? "LambdaExpression",
                LocalParams = [],
                Actions = [],
                Metadata = new NormalizedRuleMetadata
                {
                    SourceDocuments = sourceDocuments
                }
            });

            if (rulesInGroup.Count > 1)
            {
                var aliases = rulesInGroup
                    .Where(r => r != primary)
                    .Select(r => r.RuleName);
                mergeNotes.Add(
                    $"Merged {rulesInGroup.Count} rules with expression '{primary.Expression}' " +
                    $"into '{primary.RuleName}' (aliases: {string.Join(", ", aliases)}).");
                _logger.LogInformation("Merged {Count} duplicate rules with expression '{Expression}' into '{RuleName}'",
                    rulesInGroup.Count, primary.Expression, primary.RuleName);
            }
        }

        // Ensure unique rule names using PascalCase suffix convention
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in normalizedRules)
        {
            var baseName = rule.RuleName;
            var counter = 2;
            while (!seen.Add(rule.RuleName))
            {
                rule.RuleName = $"{baseName}V{counter++}";
            }
        }

        return (normalizedRules, mergeNotes);
    }

    private static string NormalizeExpression(string expression)
    {
        return expression.Trim()
            .Replace(" ", "")
            .ToLowerInvariant();
    }

    private static string BuildNormalizationNotes(
        List<CandidateRule> original, List<NormalizedRule> normalized, List<string> mergeNotes)
    {
        var notes = $"Normalized {original.Count} candidate rules into {normalized.Count} rules.";
        if (mergeNotes.Count > 0)
        {
            notes += " " + string.Join(" ", mergeNotes);
        }
        notes += " All rules use LambdaExpression type.";
        return notes;
    }
}
