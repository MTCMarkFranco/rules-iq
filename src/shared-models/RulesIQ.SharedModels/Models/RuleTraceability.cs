using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// UI-friendly traceability metadata for a single rule.
/// </summary>
public sealed class RuleTraceability
{
    [JsonPropertyName("RuleName")]
    public string RuleName { get; set; } = string.Empty;

    [JsonPropertyName("DisplayLabel")]
    public string DisplayLabel { get; set; } = string.Empty;

    [JsonPropertyName("SourceLinks")]
    public List<SourceLink> SourceLinks { get; set; } = [];

    [JsonPropertyName("TraceabilitySummary")]
    public string TraceabilitySummary { get; set; } = string.Empty;

    [JsonPropertyName("RulesetVersion")]
    public string? RulesetVersion { get; set; }

    [JsonPropertyName("RulesetPublishedTimestamp")]
    public DateTimeOffset? RulesetPublishedTimestamp { get; set; }
}

public sealed class SourceLink
{
    [JsonPropertyName("Label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("Url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("PageNumber")]
    public int? PageNumber { get; set; }

    [JsonPropertyName("Snippet")]
    public string Snippet { get; set; } = string.Empty;
}
