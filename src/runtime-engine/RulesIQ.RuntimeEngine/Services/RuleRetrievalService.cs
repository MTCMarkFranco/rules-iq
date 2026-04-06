using System.Text.Json;
using Microsoft.Extensions.Logging;
using RulesIQ.Infrastructure.Services;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.RuntimeEngine.Services;

public interface IRuleRetrievalService
{
    Task<NormalizedWorkflow> RetrieveWorkflowAsync(string workflowName, CancellationToken cancellationToken = default);
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
            if (doc.TryGetValue("RulesJson", out var rulesJsonObj) && rulesJsonObj is string rulesJsonStr)
            {
                var payload = JsonSerializer.Deserialize<IndexRulesPayload>(rulesJsonStr);
                if (payload is { HasRules: true })
                {
                    rulesetVersion ??= payload.RulesetVersion;
                    sourceDocumentVersion ??= payload.SourceDocumentVersion;
                    publishedTimestamp ??= payload.RulesetPublishedTimestamp;

                    foreach (var rule in payload.Rules)
                    {
                        allRules.Add(new NormalizedRule
                        {
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
