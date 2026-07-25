using AnkiBridge.Application.Abstractions.Images;
using AnkiBridge.Infrastructure.ExternalServices.Images.Pexels;
using AnkiBridge.Infrastructure.ExternalServices.Images.Pixabay;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Infrastructure.ExternalServices.Images;

public sealed class FallbackImageProvider(
    PixabayImageProvider pixabay,
    PexelsImageProvider pexels)
    : IImageProvider
{
    public async Task<Result<IReadOnlyList<ImageResult>>> SearchAsync(
        string keyword,
        int count = 3,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Result.Failure<IReadOnlyList<ImageResult>>("Keyword must not be empty.");

        var pixabayResult = await pixabay.SearchAsync(keyword, count, page, cancellationToken);
        if (pixabayResult is { IsSuccess: true, Value.Count: > 0 })
            return pixabayResult;

        return await pexels.SearchAsync(keyword, count, page, cancellationToken);
    }
}
