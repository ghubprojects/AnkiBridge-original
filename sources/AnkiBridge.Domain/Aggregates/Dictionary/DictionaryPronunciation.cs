using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public sealed class DictionaryPronunciation : Entity<Guid>
{
    public string Ipa { get; private set; } = default!;
    public Accent Accent { get; private set; }
    public string? AudioUrl { get; private set; }
    public AudioSource? AudioSource { get; private set; }

    #region Constructors

    private DictionaryPronunciation() { }

    private DictionaryPronunciation(
        string ipa,
        Accent accent,
        string? audioUrl,
        AudioSource? audioSource)
    {
        Id = Guid.CreateVersion7();
        Ipa = ipa;
        Accent = accent;
        AudioUrl = audioUrl;
        AudioSource = audioSource;
    }

    #endregion

    #region Factory Method

    internal static Result<DictionaryPronunciation> Create(
        string ipa,
        Accent accent,
        string? audioUrl,
        AudioSource? audioSource)
    {
        if (string.IsNullOrWhiteSpace(ipa))
            return Result.Failure<DictionaryPronunciation>("IPA must not be empty.");

        return new DictionaryPronunciation(
            ipa.Trim(),
            accent,
            audioUrl?.Trim(),
            audioSource);
    }

    #endregion
}
