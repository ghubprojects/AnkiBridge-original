using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public sealed class DictionaryTranslation : Entity<Guid>
{
    public string Text { get; private set; } = default!;
    public TranslationSource Source { get; private set; }

    #region Constructors

    private DictionaryTranslation() { }

    private DictionaryTranslation(string text, TranslationSource source)
    {
        Id = Guid.CreateVersion7();
        Text = text;
        Source = source;
    }

    #endregion

    #region Factory Method

    internal static Result<DictionaryTranslation> Create(string text, TranslationSource source)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<DictionaryTranslation>("Translation text must not be empty.");

        return new DictionaryTranslation(text.Trim(), source);
    }

    #endregion
}