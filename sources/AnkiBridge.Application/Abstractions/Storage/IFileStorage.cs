using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Abstractions.Storage;

public interface IFileStorage
{
    Task<Result<string>> UploadAsync(
        Stream fileStream,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default);
}
