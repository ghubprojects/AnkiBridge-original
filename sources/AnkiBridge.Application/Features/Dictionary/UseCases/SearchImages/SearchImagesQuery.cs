using AnkiBridge.Application.Common.Contracts.Images;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.SearchImages;

public sealed record SearchImagesQuery(
    string Keyword,
    int Count = 5,
    int Page = 1) : IRequest<Result<IReadOnlyList<ImageResult>>>;