using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// A single candidate rule extracted from a policy chunk.
/// </summary>
public sealed class CandidateRule
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

    [JsonPropertyName("Metadata")]
    public RuleMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metadata attached to a single candidate rule (pre-normalization).
/// </summary>
public sealed class RuleMetadata
{
    [JsonPropertyName("SourceDocumentId")]
    public string SourceDocumentId { get; set; } = string.Empty;

    [JsonPropertyName("SourceUri")]
    public string SourceUri { get; set; } = string.Empty;

    [JsonPropertyName("SourceDocumentVersion")]
    public string? SourceDocumentVersion { get; set; }

    [JsonPropertyName("PageNumber")]
    public int? PageNumber { get; set; }

    [JsonPropertyName("CharRange")]
    public CharRange? CharRange { get; set; }
}
