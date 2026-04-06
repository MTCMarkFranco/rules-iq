using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// Output of the extraction phase — wraps a candidate workflow with metadata.
/// </summary>
public sealed class ExtractionResult
{
    [JsonPropertyName("hasRules")]
    public bool HasRules { get; set; }

    [JsonPropertyName("workflow")]
    public CandidateWorkflow? Workflow { get; set; }

    [JsonPropertyName("RulesetVersion")]
    public string? RulesetVersion { get; set; }

    [JsonPropertyName("SourceDocumentVersion")]
    public string? SourceDocumentVersion { get; set; }

    [JsonPropertyName("extractionNotes")]
    public string ExtractionNotes { get; set; } = string.Empty;
}

/// <summary>
/// A candidate RulesEngine workflow produced from one or more chunks.
/// </summary>
public sealed class CandidateWorkflow
{
    [JsonPropertyName("WorkflowName")]
    public string WorkflowName { get; set; } = string.Empty;

    [JsonPropertyName("Rules")]
    public List<CandidateRule> Rules { get; set; } = [];
}
