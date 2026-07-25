using AnkiBridge.Application.Abstractions.Translation;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.LookupTranslations;

public sealed class LookupTranslationsQueryHandler(ITranslationProvider translationProvider)
    : IRequestHandler<LookupTranslationsQuery, Result<IReadOnlyList<TranslationResult>>>
{
    public Task<Result<IReadOnlyList<TranslationResult>>> Handle(LookupTranslationsQuery command, CancellationToken cancellationToken) =>
        translationProvider.TranslateAsync(command.Headword.Trim(), cancellationToken);
}
