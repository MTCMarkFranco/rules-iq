using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using RulesIQ.Infrastructure.Configuration;

namespace RulesIQ.Infrastructure.Services;

public interface IOpenAIClientService
{
    Task<string> GetChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}

public sealed class OpenAIClientService : IOpenAIClientService
{
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<OpenAIClientService> _logger;

    public OpenAIClientService(IOptions<AzureOpenAIOptions> options, ILogger<OpenAIClientService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new AzureOpenAIClient(new Uri(_options.Endpoint), new DefaultAzureCredential());
    }

    public async Task<string> GetChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(_options.DeploymentName);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completionOptions = new ChatCompletionOptions
        {
            Temperature = 0f,
            MaxOutputTokenCount = 4096,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        _logger.LogInformation("Calling OpenAI deployment {Deployment}", _options.DeploymentName);
        var response = await chatClient.CompleteChatAsync(messages, completionOptions, cancellationToken);
        return response.Value.Content[0].Text;
    }
}
