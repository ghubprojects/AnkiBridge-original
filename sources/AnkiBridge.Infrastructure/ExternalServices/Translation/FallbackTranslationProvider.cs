using AnkiBridge.Application.Abstractions.Translation;
using AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;
using AnkiBridge.Infrastructure.ExternalServices.Translation.Google;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Infrastructure.ExternalServices.Translation;

public sealed class FallbackTranslationProvider(
    CambridgeDictionaryTranslationProvider cambridge,
    GoogleTranslationProvider google)
    : ITranslationProvider
{
    public async Task<Result<IReadOnlyList<TranslationResult>>> TranslateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<IReadOnlyList<TranslationResult>>("Text must not be empty.");

        var cambridgeResult = await cambridge.TranslateAsync(text, cancellationToken);
        if (cambridgeResult is { IsSuccess: true, Value.Count: > 0 })
            return cambridgeResult;

        return await google.TranslateAsync(text, cancellationToken);
    }
}
