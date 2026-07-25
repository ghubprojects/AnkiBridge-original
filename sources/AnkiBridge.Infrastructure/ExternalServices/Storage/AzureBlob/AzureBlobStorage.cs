using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AnkiBridge.Shared.Results;
using AnkiBridge.Application.Common.Contracts.Storage;
using Microsoft.Extensions.Options;
using AnkiBridge.Infrastructure.ExternalServices.Storage.Azure;

namespace AnkiBridge.Infrastructure.ExternalServices.Storage.AzureBlob;

public class AzureBlobStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private readonly Uri? _publicEndpoint;

    public AzureBlobStorage(
        BlobServiceClient blobServiceClient,
        IOptions<AzureBlobStorageOptions> options)
    {
        _container = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        _publicEndpoint = options.Value.PublicEndpoint;
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<Result<string>> UploadAsync(
        Stream fileStream,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _container.GetBlobClient(blobName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            await blobClient.UploadAsync(fileStream, options, cancellationToken);

            return Result<string>.Success(GetPublicUri(blobClient.Uri));
        }
        catch (RequestFailedException ex)
        {
            return Result<string>.Failure($"Azure error: {ex.Message}");
        }
    }

    private string GetPublicUri(Uri blobUri)
    {
        if (_publicEndpoint is null)
            return blobUri.AbsoluteUri;

        var publicUri = new UriBuilder(blobUri)
        {
            Scheme = _publicEndpoint.Scheme,
            Host = _publicEndpoint.Host,
            Port = _publicEndpoint.IsDefaultPort ? -1 : _publicEndpoint.Port
        };

        return publicUri.Uri.AbsoluteUri;
    }
}
