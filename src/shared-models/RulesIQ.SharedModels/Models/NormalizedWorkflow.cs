using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// A fully normalized RulesEngine workflow ready for indexing and runtime execution.
/// </summary>
public sealed class NormalizedWorkflow
{
    [JsonPropertyName("WorkflowName")]
    public string WorkflowName { get; set; } = string.Empty;

    [JsonPropertyName("RulesetVersion")]
    public string? RulesetVersion { get; set; }

    [JsonPropertyName("SourceDocumentVersion")]
    public string? SourceDocumentVersion { get; set; }

    [JsonPropertyName("RulesetPublishedTimestamp")]
    public DateTimeOffset? RulesetPublishedTimestamp { get; set; }

    [JsonPropertyName("Rules")]
    public List<NormalizedRule> Rules { get; set; } = [];

    [JsonPropertyName("NormalizationNotes")]
    public string NormalizationNotes { get; set; } = string.Empty;
}

/// <summary>
/// A single normalized rule with aggregated source metadata.
/// </summary>
public sealed class NormalizedRule
{
    [JsonPropertyName("RuleName")]
    public string RuleName { get; set; } = string.Empty;

    [JsonPropertyName("Expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("SuccessEvent")]
    public string SuccessEvent { get; set; } = string.Empty;

    [JsonPropertyName("ErrorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("RuleExpressionType")]
    public string RuleExpressionType { get; set; } = "LambdaExpression";

    [JsonPropertyName("LocalParams")]
    public List<object> LocalParams { get; set; } = [];

    [JsonPropertyName("Actions")]
    public List<object> Actions { get; set; } = [];

    [JsonPropertyName("Metadata")]
    public NormalizedRuleMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Aggregated metadata for a normalized rule — may trace to multiple source documents.
/// </summary>
public sealed class NormalizedRuleMetadata
{
    [JsonPropertyName("SourceDocuments")]
    public List<SourceDocumentMetadata> SourceDocuments { get; set; } = [];
}
