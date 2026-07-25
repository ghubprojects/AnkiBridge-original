using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Abstractions.Translation;

public interface ITranslationProvider
{
    Task<Result<IReadOnlyList<TranslationResult>>> TranslateAsync(
        string text,
        CancellationToken cancellationToken = default);
}
