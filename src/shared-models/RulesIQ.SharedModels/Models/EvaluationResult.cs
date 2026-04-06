using System.Text.Json.Serialization;

namespace RulesIQ.SharedModels.Models;

/// <summary>
/// Complete evaluation result produced by the runtime engine.
/// </summary>
public sealed class EvaluationResult
{
    [JsonPropertyName("EvaluationId")]
    public string EvaluationId { get; set; } = string.Empty;

    [JsonPropertyName("WorkflowName")]
    public string WorkflowName { get; set; } = string.Empty;

    [JsonPropertyName("EvaluationGoal")]
    public string EvaluationGoal { get; set; } = string.Empty;

    [JsonPropertyName("VersionFingerprint")]
    public VersionFingerprint VersionFingerprint { get; set; } = new();

    [JsonPropertyName("ComplianceScore")]
    public ComplianceScore ComplianceScore { get; set; } = new();

    [JsonPropertyName("RulesSnapshot")]
    public RulesSnapshot RulesSnapshot { get; set; } = new();
}

public sealed class VersionFingerprint
{
    [JsonPropertyName("RulesetVersion")]
    public string? RulesetVersion { get; set; }

    [JsonPropertyName("RulesetPublishedTimestamp")]
    public DateTimeOffset? RulesetPublishedTimestamp { get; set; }

    [JsonPropertyName("SourceDocumentVersions")]
    public List<SourceDocumentVersionEntry> SourceDocumentVersions { get; set; } = [];

    [JsonPropertyName("EvaluationTimestamp")]
    public DateTimeOffset EvaluationTimestamp { get; set; }
}

public sealed class SourceDocumentVersionEntry
{
    [JsonPropertyName("SourceDocumentId")]
    public string SourceDocumentId { get; set; } = string.Empty;

    [JsonPropertyName("SourceDocumentVersion")]
    public string? SourceDocumentVersion { get; set; }

    [JsonPropertyName("IngestedTimestamp")]
    public DateTimeOffset? IngestedTimestamp { get; set; }
}

public sealed class ComplianceScore
{
    [JsonPropertyName("TotalRulesEvaluated")]
    public int TotalRulesEvaluated { get; set; }

    [JsonPropertyName("RulesPassed")]
    public int RulesPassed { get; set; }

    [JsonPropertyName("RulesFailed")]
    public int RulesFailed { get; set; }

    [JsonPropertyName("CompliancePercentage")]
    public decimal CompliancePercentage { get; set; }

    [JsonPropertyName("FailedRules")]
    public List<FailedRuleEntry> FailedRules { get; set; } = [];
}

public sealed class FailedRuleEntry
{
    [JsonPropertyName("RuleName")]
    public string RuleName { get; set; } = string.Empty;

    [JsonPropertyName("Expression")]
    public string? Expression { get; set; }

    [JsonPropertyName("ErrorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class RulesSnapshot
{
    [JsonPropertyName("WorkflowName")]
    public string WorkflowName { get; set; } = string.Empty;

    [JsonPropertyName("RulesetVersion")]
    public string? RulesetVersion { get; set; }

    [JsonPropertyName("Rules")]
    public List<RuleSnapshotEntry> Rules { get; set; } = [];
}

public sealed class RuleSnapshotEntry
{
    [JsonPropertyName("RuleName")]
    public string RuleName { get; set; } = string.Empty;

    [JsonPropertyName("Expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("Result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("SuccessEvent")]
    public string? SuccessEvent { get; set; }

    [JsonPropertyName("ErrorMessage")]
    public string? ErrorMessage { get; set; }
}
