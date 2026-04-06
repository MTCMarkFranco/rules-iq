namespace RulesIQ.Infrastructure.Configuration;

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4.1";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-large";
}

public sealed class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";
    public string Endpoint { get; set; } = string.Empty;
    public string IndexName { get; set; } = "idx-rules-iq";
}

public sealed class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";
    public string AccountName { get; set; } = "sadatafileshubcanada";
    public string ContainerName { get; set; } = "policy-documents";
}
