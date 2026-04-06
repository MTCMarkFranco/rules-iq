using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// Chunk produced by the ingestion/preprocessing phase.
/// </summary>
public sealed class PolicyChunk
{
    [JsonPropertyName("chunk_id")]
    public string ChunkId { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("page_number")]
    public int? PageNumber { get; set; }

    [JsonPropertyName("char_range")]
    public CharRange? CharRange { get; set; }

    [JsonPropertyName("semantic_label")]
    public string? SemanticLabel { get; set; }
}

/// <summary>
/// Result of the ingestion/preprocessing phase.
/// </summary>
public sealed class IngestionResult
{
    [JsonPropertyName("chunks")]
    public List<PolicyChunk> Chunks { get; set; } = [];

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}
