using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// Azure AI Search custom skill request envelope.
/// </summary>
public sealed class SkillsetRequest
{
    [JsonPropertyName("values")]
    public List<SkillsetRequestRecord> Values { get; set; } = [];
}

public sealed class SkillsetRequestRecord
{
    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public SkillsetRequestData Data { get; set; } = new();
}

public sealed class SkillsetRequestData
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("document_id")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("source_uri")]
    public string SourceUri { get; set; } = string.Empty;

    [JsonPropertyName("page_number")]
    public int? PageNumber { get; set; }

    [JsonPropertyName("char_range")]
    public CharRange? CharRange { get; set; }

    [JsonPropertyName("workflow_hint")]
    public string? WorkflowHint { get; set; }

    [JsonPropertyName("source_document_version")]
    public string? SourceDocumentVersion { get; set; }

    [JsonPropertyName("ruleset_version")]
    public string? RulesetVersion { get; set; }
}

/// <summary>
/// Azure AI Search custom skill response envelope.
/// </summary>
public sealed class SkillsetResponse
{
    [JsonPropertyName("values")]
    public List<SkillsetResponseRecord> Values { get; set; } = [];
}

public sealed class SkillsetResponseRecord
{
    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public SkillsetResponseData Data { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<SkillsetError>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<SkillsetWarning>? Warnings { get; set; }
}

public sealed class SkillsetResponseData
{
    [JsonPropertyName("rulesJson")]
    public string? RulesJson { get; set; }

    [JsonPropertyName("hasRules")]
    public bool HasRules { get; set; }

    [JsonPropertyName("ruleCount")]
    public int RuleCount { get; set; }

    [JsonPropertyName("workflowName")]
    public string? WorkflowName { get; set; }
}

public sealed class SkillsetError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public sealed class SkillsetWarning
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
