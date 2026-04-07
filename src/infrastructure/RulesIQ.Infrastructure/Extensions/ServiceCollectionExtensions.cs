using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RulesIQ.Infrastructure.Configuration;
using RulesIQ.Infrastructure.Services;

namespace RulesIQ.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRulesIQInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<AzureSearchOptions>(configuration.GetSection(AzureSearchOptions.SectionName));
        services.Configure<AzureBlobOptions>(configuration.GetSection(AzureBlobOptions.SectionName));

        services.AddSingleton<IOpenAIClientService, OpenAIClientService>();
        services.AddSingleton<ISearchClientService, SearchClientService>();
        services.AddSingleton<IBlobSasService, BlobSasService>();

        return services;
    }
}
