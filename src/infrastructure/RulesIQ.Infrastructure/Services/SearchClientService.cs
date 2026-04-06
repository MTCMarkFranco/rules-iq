using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RulesIQ.Infrastructure.Configuration;
using System.Text.Json;

namespace RulesIQ.Infrastructure.Services;

public interface ISearchClientService
{
    Task<List<SearchDocument>> SearchRulesAsync(string workflowName, CancellationToken cancellationToken = default);
    Task<List<SearchDocument>> SearchRulesByDocumentAsync(string documentId, CancellationToken cancellationToken = default);
}

public sealed class SearchClientService : ISearchClientService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger<SearchClientService> _logger;

    public SearchClientService(IOptions<AzureSearchOptions> options, ILogger<SearchClientService> logger)
    {
        _logger = logger;
        var searchOptions = options.Value;
        _searchClient = new SearchClient(
            new Uri(searchOptions.Endpoint),
            searchOptions.IndexName,
            new DefaultAzureCredential());
    }

    public async Task<List<SearchDocument>> SearchRulesAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching rules for workflow: {WorkflowName}", workflowName);
        var searchOptions = new SearchOptions
        {
            Filter = $"HasRules eq true and WorkflowName eq '{workflowName}'",
            Select = { "RulesJson", "Content", "SourceUri", "PageNumber", "RulesetVersion", "SourceDocumentVersion", "SourceDocumentId" },
            Size = 100
        };

        var results = new List<SearchDocument>();
        var response = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions, cancellationToken);
        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add(result.Document);
        }

        _logger.LogInformation("Found {Count} rule chunks for workflow {WorkflowName}", results.Count, workflowName);
        return results;
    }

    public async Task<List<SearchDocument>> SearchRulesByDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching rules for document: {DocumentId}", documentId);
        var searchOptions = new SearchOptions
        {
            Filter = $"HasRules eq true and SourceDocumentId eq '{documentId}'",
            Select = { "RulesJson", "Content", "PageNumber", "RulesetVersion", "SourceDocumentVersion" },
            Size = 100
        };

        var results = new List<SearchDocument>();
        var response = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions, cancellationToken);
        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add(result.Document);
        }

        return results;
    }
}
