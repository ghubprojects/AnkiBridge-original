using AnkiBridge.Application.Abstractions.Images;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateImages;

public sealed class GenerateImagesQueryHandler(IImageProvider imageProvider)
    : IRequestHandler<GenerateImagesQuery, Result<IReadOnlyList<ImageResult>>>
{
    public Task<Result<IReadOnlyList<ImageResult>>> Handle(GenerateImagesQuery request, CancellationToken cancellationToken) =>
        imageProvider.SearchAsync(
            request.Keyword.Trim(),
            request.Count,
            request.Page,
            cancellationToken);
}
