using AnkiBridge.Application.Common.Contracts.Images;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.SearchImages;

public sealed class SearchImagesQueryHandler(IImageProvider imageProvider)
    : IRequestHandler<SearchImagesQuery, Result<IReadOnlyList<ImageResult>>>
{
    public async Task<Result<IReadOnlyList<ImageResult>>> Handle(
        SearchImagesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return Result.Failure<IReadOnlyList<ImageResult>>("Keyword must not be empty.");

        var imageResults = await imageProvider.SearchAsync(
            request.Keyword.Trim(),
            request.Count,
            request.Page,
            cancellationToken);

        return Result.Success(imageResults);
    }
}