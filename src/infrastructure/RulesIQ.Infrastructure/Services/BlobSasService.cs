using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RulesIQ.Infrastructure.Configuration;

namespace RulesIQ.Infrastructure.Services;

public interface IBlobSasService
{
    Task<string> GetBlobSasUrlAsync(string blobUri, CancellationToken ct = default);
}

public sealed class BlobSasService : IBlobSasService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly ILogger<BlobSasService> _logger;

    public BlobSasService(IOptions<AzureBlobOptions> options, ILogger<BlobSasService> logger)
    {
        _logger = logger;
        var blobOptions = options.Value;
        var serviceUri = new Uri($"https://{blobOptions.AccountName}.blob.core.windows.net");
        _serviceClient = new BlobServiceClient(serviceUri, new DefaultAzureCredential());
    }

    public async Task<string> GetBlobSasUrlAsync(string blobUri, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(blobUri))
            return string.Empty;

        try
        {
            var uri = new Uri(blobUri);
            // Extract container and blob path from the URI
            // Format: https://<account>.blob.core.windows.net/<container>/<blob-path>
            var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            if (segments.Length < 2)
                return blobUri;

            var containerName = segments[0];
            var blobName = Uri.UnescapeDataString(segments[1]);

            var delegationKey = await _serviceClient.GetUserDelegationKeyAsync(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddHours(1),
                ct);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var blobUriBuilder = new BlobUriBuilder(uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(delegationKey, _serviceClient.AccountName)
            };

            _logger.LogDebug("Generated SAS URL for blob {BlobName}", blobName);
            return blobUriBuilder.ToUri().ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate SAS URL for {BlobUri}, returning original", blobUri);
            return blobUri;
        }
    }
}
