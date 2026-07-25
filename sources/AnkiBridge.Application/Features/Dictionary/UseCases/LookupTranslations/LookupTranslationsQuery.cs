using AnkiBridge.Application.Abstractions.Translation;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.LookupTranslations;

public sealed record LookupTranslationsQuery(string Headword)
    : IRequest<Result<IReadOnlyList<TranslationResult>>>;
