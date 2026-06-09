using AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Logging;

namespace AnkiBridge.Infrastructure.Services.Scraping;

public sealed class FallbackTranslationProvider(
    CambridgeTranslationProvider cambridge,
    GoogleTranslationProvider google,
    ILogger<FallbackTranslationProvider> logger)
    : ITranslationProvider
{
    public async Task<Result<IReadOnlyList<TranslationLookupResult>>> GetTranslationsAsync(
        string word,
        CancellationToken ct = default)
    {
        var primary = await cambridge.GetTranslationsAsync(word, ct);

        if (primary.IsSuccess && primary.Value.Count > 0)
        {
            logger.LogDebug("Cambridge EV resolved {Count} translations for word={Word}",
                primary.Value.Count, word);
            return primary;
        }

        logger.LogDebug("Cambridge EV found nothing for word={Word}, falling back to Google", word);
        return await google.GetTranslationsAsync(word, ct);
    }
}