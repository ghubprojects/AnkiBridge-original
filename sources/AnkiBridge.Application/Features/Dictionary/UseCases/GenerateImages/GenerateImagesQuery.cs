using AnkiBridge.Application.Abstractions.Images;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateImages;

public sealed record GenerateImagesQuery(string Keyword, int Count = 3, int Page = 1)
    : IRequest<Result<IReadOnlyList<ImageResult>>>;
