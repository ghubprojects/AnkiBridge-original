using AnkiBridge.Infrastructure.ExternalServices.Storage.AzureBlob;

namespace AnkiBridge.Infrastructure.ExternalServices.Storage.Azure;

/// <summary>
/// Configuration for <see cref="AzureBlobStorage"/>.
/// Bind from appsettings section <c>Storage:AzureBlob</c>.
/// </summary>
public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "Storage:AzureBlob";

    /// <summary>
    /// The container used to store uploaded blobs.
    /// </summary>
    public string ContainerName { get; init; } = "uploads";

    /// <summary>
    /// Optional public service endpoint used in URLs returned after upload.
    /// </summary>
    public Uri? PublicEndpoint { get; init; }
}
