using System.Text.Json;
using Microsoft.Extensions.Logging;
using RulesIQ.Infrastructure.Services;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.UIAdapter.Services;

/// <summary>
/// A single index document (chunk) with its parsed rules, ready for management UI editing.
/// </summary>
public sealed class ManagedDocument
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SourceUri { get; set; } = string.Empty;
    public string SourceDocumentId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string? RulesetVersion { get; set; }
    public string? SourceDocumentVersion { get; set; }
    public int? PageNumber { get; set; }
    public string? SemanticLabel { get; set; }
    public DateTimeOffset? IndexedTimestamp { get; set; }
    public DateTimeOffset? RulesetPublishedTimestamp { get; set; }
    public List<ManagedRule> Rules { get; set; } = [];
    public bool IsDirty { get; set; }
}

/// <summary>
/// An editable rule within a managed document.
/// </summary>
public sealed class ManagedRule
{
    public string RuleName { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string SuccessEvent { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string RuleExpressionType { get; set; } = "LambdaExpression";
    public RuleMetadata OriginalMetadata { get; set; } = new();
}

public interface IRuleManagementService
{
    Task<List<ManagedDocument>> LoadAllRuleDocumentsAsync(CancellationToken ct = default);
    Task SaveDocumentAsync(ManagedDocument document, CancellationToken ct = default);
}

public sealed class RuleManagementService : IRuleManagementService
{
    private readonly ISearchClientService _searchClient;
    private readonly ILogger<RuleManagementService> _logger;

    public RuleManagementService(ISearchClientService searchClient, ILogger<RuleManagementService> logger)
    {
        _searchClient = searchClient;
        _logger = logger;
    }

    public async Task<List<ManagedDocument>> LoadAllRuleDocumentsAsync(CancellationToken ct = default)
    {
        var documents = await _searchClient.SearchAllDocumentsForManagementAsync(ct);
        var result = new List<ManagedDocument>();

        foreach (var doc in documents)
        {
            var managed = new ManagedDocument
            {
                Id = doc.TryGetValue("id", out var id) ? id?.ToString() ?? "" : "",
                Content = doc.TryGetValue("Content", out var content) ? content?.ToString() ?? "" : "",
                SourceUri = doc.TryGetValue("SourceUri", out var uri) ? uri?.ToString() ?? "" : "",
                SourceDocumentId = doc.TryGetValue("SourceDocumentId", out var docId) ? docId?.ToString() ?? "" : "",
                WorkflowName = doc.TryGetValue("WorkflowName", out var wf) ? wf?.ToString() ?? "" : "",
                RulesetVersion = doc.TryGetValue("RulesetVersion", out var rv) ? rv?.ToString() : null,
                SourceDocumentVersion = doc.TryGetValue("SourceDocumentVersion", out var sdv) ? sdv?.ToString() : null,
                PageNumber = doc.TryGetValue("PageNumber", out var pn) && pn is int pageNum ? pageNum : (int?)null,
                SemanticLabel = doc.TryGetValue("SemanticLabel", out var sl) ? sl?.ToString() : null,
                IndexedTimestamp = doc.TryGetValue("IndexedTimestamp", out var it) && it is DateTimeOffset ts ? ts : (DateTimeOffset?)null,
                RulesetPublishedTimestamp = doc.TryGetValue("RulesetPublishedTimestamp", out var rpt) && rpt is DateTimeOffset rpts ? rpts : (DateTimeOffset?)null,
            };

            if (doc.TryGetValue("RulesJson", out var rulesJsonObj) && rulesJsonObj is string rulesJsonStr)
            {
                var payload = JsonSerializer.Deserialize<IndexRulesPayload>(rulesJsonStr);
                if (payload is { HasRules: true })
                {
                    managed.Rules = payload.Rules.Select(r => new ManagedRule
                    {
                        RuleName = r.RuleName,
                        Expression = r.Expression,
                        SuccessEvent = r.SuccessEvent,
                        ErrorMessage = r.ErrorMessage,
                        RuleExpressionType = r.RuleExpressionType,
                        OriginalMetadata = r.Metadata
                    }).ToList();
                }
            }

            if (managed.Rules.Count > 0)
            {
                result.Add(managed);
            }
        }

        _logger.LogInformation("Loaded {Count} documents with {RuleCount} total rules for management",
            result.Count, result.Sum(d => d.Rules.Count));
        return result;
    }

    public async Task SaveDocumentAsync(ManagedDocument document, CancellationToken ct = default)
    {
        var payload = new IndexRulesPayload
        {
            HasRules = document.Rules.Count > 0,
            WorkflowName = document.WorkflowName,
            RulesetVersion = document.RulesetVersion,
            SourceDocumentVersion = document.SourceDocumentVersion,
            RulesetPublishedTimestamp = document.RulesetPublishedTimestamp,
            Rules = document.Rules.Select(r => new CandidateRule
            {
                RuleName = r.RuleName,
                Expression = r.Expression,
                SuccessEvent = r.SuccessEvent,
                ErrorMessage = r.ErrorMessage,
                RuleExpressionType = r.RuleExpressionType,
                Metadata = r.OriginalMetadata
            }).ToList()
        };

        var rulesJson = JsonSerializer.Serialize(payload);

        var fields = new Dictionary<string, object>
        {
            ["id"] = document.Id,
            ["RulesJson"] = rulesJson,
            ["RuleCount"] = document.Rules.Count,
            ["HasRules"] = document.Rules.Count > 0
        };

        await _searchClient.MergeDocumentAsync(fields, ct);

        _logger.LogInformation("Saved {RuleCount} rules for document {DocumentId}", document.Rules.Count, document.Id);
    }
}
