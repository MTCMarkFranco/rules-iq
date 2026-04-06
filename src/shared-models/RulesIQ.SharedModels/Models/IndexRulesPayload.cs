using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// The RulesJson payload stored in the Azure AI Search index per chunk.
/// </summary>
public sealed class IndexRulesPayload
{
    [JsonPropertyName("hasRules")]
    public bool HasRules { get; set; }

    [JsonPropertyName("WorkflowName")]
    public string? WorkflowName { get; set; }

    [JsonPropertyName("RulesetVersion")]
    public string? RulesetVersion { get; set; }

    [JsonPropertyName("SourceDocumentVersion")]
    public string? SourceDocumentVersion { get; set; }

    [JsonPropertyName("RulesetPublishedTimestamp")]
    public DateTimeOffset? RulesetPublishedTimestamp { get; set; }

    [JsonPropertyName("Rules")]
    public List<CandidateRule> Rules { get; set; } = [];
}
