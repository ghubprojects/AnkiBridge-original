using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;

public interface ITranslationProvider
{
    Task<Result<IReadOnlyList<TranslationLookupResult>>> GetTranslationsAsync(
        string word,
        CancellationToken ct = default);
}

public sealed record TranslationLookupResult(string Text, TranslationSource Source);