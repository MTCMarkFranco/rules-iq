using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// Represents source document traceability metadata for a single chunk.
/// </summary>
public sealed class SourceDocumentMetadata
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

public sealed class CharRange
{
    [JsonPropertyName("Start")]
    public int Start { get; set; }

    [JsonPropertyName("End")]
    public int End { get; set; }
}
