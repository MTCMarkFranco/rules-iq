using System.Text.Json;
using Microsoft.Extensions.Logging;
using RulesIQ.Infrastructure.Services;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.RuntimeEngine.Services;

public interface IRuleRetrievalService
{
    Task<NormalizedWorkflow> RetrieveWorkflowAsync(string workflowName, CancellationToken cancellationToken = default);
    Task<NormalizedWorkflow> RetrieveAllRulesAsync(CancellationToken cancellationToken = default);
}

public sealed class RuleRetrievalService : IRuleRetrievalService
{
    private readonly ISearchClientService _searchClient;
    private readonly ILogger<RuleRetrievalService> _logger;

    public RuleRetrievalService(ISearchClientService searchClient, ILogger<RuleRetrievalService> logger)
    {
        _searchClient = searchClient;
        _logger = logger;
    }

    public async Task<NormalizedWorkflow> RetrieveAllRulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all rules from search index");

        var documents = await _searchClient.SearchAllRulesAsync(cancellationToken);

        var allRules = new List<NormalizedRule>();
        string? rulesetVersion = null;
        string? sourceDocumentVersion = null;
        DateTimeOffset? publishedTimestamp = null;

        foreach (var doc in documents)
        {
            var docId = doc.TryGetValue("id", out var idObj) ? idObj?.ToString() ?? "" : "";

            if (doc.TryGetValue("RulesJson", out var rulesJsonObj) && rulesJsonObj is string rulesJsonStr)
            {
                var payload = JsonSerializer.Deserialize<IndexRulesPayload>(rulesJsonStr);
                if (payload is { HasRules: true })
                {
                    rulesetVersion ??= payload.RulesetVersion;
                    sourceDocumentVersion ??= payload.SourceDocumentVersion;
                    publishedTimestamp ??= payload.RulesetPublishedTimestamp;

                    for (int i = 0; i < payload.Rules.Count; i++)
                    {
                        var rule = payload.Rules[i];
                        allRules.Add(new NormalizedRule
                        {
                            Id = $"{docId}_{i + 1}",
                            RuleName = rule.RuleName,
                            Expression = rule.Expression,
                            SuccessEvent = rule.SuccessEvent,
                            ErrorMessage = rule.ErrorMessage,
                            RuleExpressionType = rule.RuleExpressionType,
                            Metadata = new NormalizedRuleMetadata
                            {
                                SourceDocuments =
                                [
                                    new SourceDocumentMetadata
                                    {
                                        SourceDocumentId = rule.Metadata.SourceDocumentId,
                                        SourceUri = rule.Metadata.SourceUri,
                                        SourceDocumentVersion = rule.Metadata.SourceDocumentVersion,
                                        PageNumber = rule.Metadata.PageNumber,
                                        CharRange = rule.Metadata.CharRange
                                    }
                                ]
                            }
                        });
                    }
                }
            }
        }

        _logger.LogInformation("Retrieved {Count} rules from index", allRules.Count);

        return new NormalizedWorkflow
        {
            WorkflowName = "AllIndexedRules",
            RulesetVersion = rulesetVersion,
            SourceDocumentVersion = sourceDocumentVersion,
            RulesetPublishedTimestamp = publishedTimestamp,
            Rules = allRules
        };
    }

    public async Task<NormalizedWorkflow> RetrieveWorkflowAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving workflow {WorkflowName} from search index", workflowName);

        var documents = await _searchClient.SearchRulesAsync(workflowName, cancellationToken);

        var allRules = new List<NormalizedRule>();
        string? rulesetVersion = null;
        string? sourceDocumentVersion = null;
        DateTimeOffset? publishedTimestamp = null;

        foreach (var doc in documents)
        {
            var docId = doc.TryGetValue("id", out var idObj) ? idObj?.ToString() ?? "" : "";

            if (doc.TryGetValue("RulesJson", out var rulesJsonObj) && rulesJsonObj is string rulesJsonStr)
            {
                var payload = JsonSerializer.Deserialize<IndexRulesPayload>(rulesJsonStr);
                if (payload is { HasRules: true })
                {
                    rulesetVersion ??= payload.RulesetVersion;
                    sourceDocumentVersion ??= payload.SourceDocumentVersion;
                    publishedTimestamp ??= payload.RulesetPublishedTimestamp;

                    for (int i = 0; i < payload.Rules.Count; i++)
                    {
                        var rule = payload.Rules[i];
                        allRules.Add(new NormalizedRule
                        {
                            Id = $"{docId}_{i + 1}",
                            RuleName = rule.RuleName,
                            Expression = rule.Expression,
                            SuccessEvent = rule.SuccessEvent,
                            ErrorMessage = rule.ErrorMessage,
                            RuleExpressionType = rule.RuleExpressionType,
                            Metadata = new NormalizedRuleMetadata
                            {
                                SourceDocuments =
                                [
                                    new SourceDocumentMetadata
                                    {
                                        SourceDocumentId = rule.Metadata.SourceDocumentId,
                                        SourceUri = rule.Metadata.SourceUri,
                                        SourceDocumentVersion = rule.Metadata.SourceDocumentVersion,
                                        PageNumber = rule.Metadata.PageNumber,
                                        CharRange = rule.Metadata.CharRange
                                    }
                                ]
                            }
                        });
                    }
                }
            }
        }

        _logger.LogInformation("Retrieved {Count} rules for workflow {WorkflowName}", allRules.Count, workflowName);

        return new NormalizedWorkflow
        {
            WorkflowName = workflowName,
            RulesetVersion = rulesetVersion,
            SourceDocumentVersion = sourceDocumentVersion,
            RulesetPublishedTimestamp = publishedTimestamp,
            Rules = allRules
        };
    }
}
