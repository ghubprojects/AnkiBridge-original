using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Abstractions.Images;

public interface IImageProvider
{
    Task<Result<IReadOnlyList<ImageResult>>> SearchAsync(
        string keyword,
        int count = 3,
        int page = 1,
        CancellationToken cancellationToken = default);
}
