using System.Text.Json;
using Microsoft.Extensions.Logging;
using RulesIQ.Infrastructure.Services;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.IndexerSkill.Services;

public interface IRuleExtractionService
{
    Task<ExtractionResult> ExtractRulesAsync(SkillsetRequestData data, CancellationToken cancellationToken = default);
}

public sealed class RuleExtractionService : IRuleExtractionService
{
    private readonly IOpenAIClientService _openAIClient;
    private readonly ILogger<RuleExtractionService> _logger;

    private const string SystemPrompt = """
        You are part of an offline, deterministic rules compilation pipeline.
        Your job is to convert unstructured policy text into candidate business rules 
        compatible with the Microsoft .NET RulesEngine JSON schema.
        
        RULES:
        - Output a single JSON object with: hasRules (bool), workflow (object or null), extractionNotes (string).
        - If hasRules is true, workflow must have WorkflowName (string) and Rules (array).
        - Each rule must have: RuleName, Expression (valid C# boolean expression using input.Property), SuccessEvent, ErrorMessage, Metadata.
        - Metadata must include: SourceDocumentId, SourceUri, SourceDocumentVersion, PageNumber, CharRange.
        - Do NOT invent thresholds or conditions not present in the text.
        - Do NOT convert non-deterministic language ("at the discretion of", "where appropriate") into rules.
        - Each rule should be atomic — one logical condition per rule.
        - Expressions must reference an abstract "input" object (e.g., input.Age >= 18).
        - If the chunk contains no enforceable rules, set hasRules=false, workflow=null.
        - Include extractionNotes explaining your reasoning.
        - If RulesetVersion or SourceDocumentVersion is provided, include them in the output.
        """;

    public RuleExtractionService(IOpenAIClientService openAIClient, ILogger<RuleExtractionService> logger)
    {
        _openAIClient = openAIClient;
        _logger = logger;
    }

    public async Task<ExtractionResult> ExtractRulesAsync(SkillsetRequestData data, CancellationToken cancellationToken = default)
    {
        var userPrompt = BuildUserPrompt(data);
        _logger.LogInformation("Extracting rules from chunk for document {DocumentId}", data.DocumentId);

        try
        {
            var response = await _openAIClient.GetChatCompletionAsync(SystemPrompt, userPrompt, cancellationToken);
            var result = JsonSerializer.Deserialize<ExtractionResult>(response);

            if (result is null)
            {
                _logger.LogWarning("Failed to deserialize extraction result for document {DocumentId}", data.DocumentId);
                return new ExtractionResult
                {
                    HasRules = false,
                    ExtractionNotes = "Failed to deserialize OpenAI response."
                };
            }

            _logger.LogInformation("Extracted {HasRules} rules from document {DocumentId}",
                result.HasRules ? result.Workflow?.Rules.Count ?? 0 : 0, data.DocumentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting rules from document {DocumentId}", data.DocumentId);
            return new ExtractionResult
            {
                HasRules = false,
                ExtractionNotes = $"Extraction failed: {ex.Message}"
            };
        }
    }

    private static string BuildUserPrompt(SkillsetRequestData data)
    {
        var metadata = new
        {
            document_id = data.DocumentId,
            source_uri = data.SourceUri,
            page_number = data.PageNumber,
            char_range = data.CharRange,
            workflow_hint = data.WorkflowHint,
            source_document_version = data.SourceDocumentVersion,
            ruleset_version = data.RulesetVersion
        };

        return $"""
            Extract candidate rules from the following policy chunk.
            
            CHUNK METADATA:
            {JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true })}
            
            POLICY CHUNK:
            {data.Content}
            """;
    }
}
