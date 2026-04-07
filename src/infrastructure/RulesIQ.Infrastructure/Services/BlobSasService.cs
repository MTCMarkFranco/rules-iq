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
    private readonly SemaphoreSlim _delegationKeyLock = new(1, 1);
    private Azure.Response<Azure.Storage.Blobs.Models.UserDelegationKey>? _cachedDelegationKey;
    private DateTimeOffset _delegationKeyExpiry;

    public BlobSasService(IOptions<AzureBlobOptions> options, ILogger<BlobSasService> logger)
    {
        _logger = logger;
        var blobOptions = options.Value;
        var serviceUri = new Uri($"https://{blobOptions.AccountName}.blob.core.windows.net");
        _serviceClient = new BlobServiceClient(serviceUri, new DefaultAzureCredential());
    }

    private async Task<Azure.Response<Azure.Storage.Blobs.Models.UserDelegationKey>> GetCachedDelegationKeyAsync(CancellationToken ct)
    {
        if (_cachedDelegationKey is not null && DateTimeOffset.UtcNow < _delegationKeyExpiry)
            return _cachedDelegationKey;

        await _delegationKeyLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cachedDelegationKey is not null && DateTimeOffset.UtcNow < _delegationKeyExpiry)
                return _cachedDelegationKey;

            var expiry = DateTimeOffset.UtcNow.AddHours(1);
            _cachedDelegationKey = await _serviceClient.GetUserDelegationKeyAsync(
                DateTimeOffset.UtcNow.AddMinutes(-5), expiry, ct);
            _delegationKeyExpiry = expiry.AddMinutes(-5); // refresh 5 min before expiry
            return _cachedDelegationKey;
        }
        finally
        {
            _delegationKeyLock.Release();
        }
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

            var delegationKey = await GetCachedDelegationKeyAsync(ct);

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
