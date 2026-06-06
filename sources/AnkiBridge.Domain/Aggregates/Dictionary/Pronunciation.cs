using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public class Pronunciation : Entity<Guid>
{
    public string Ipa { get; private set; } = default!;
    public Accent Accent { get; private set; }
    public string AudioUrl { get; private set; } = default!;
    public AudioSource AudioSource { get; private set; }

    private Pronunciation(
        string ipa,
        Accent accent,
        string audioUrl,
        AudioSource audioSource)
    {
        Id = Guid.CreateVersion7();
        Ipa = ipa;
        Accent = accent;
        AudioUrl = audioUrl;
        AudioSource = audioSource;
    }

    internal static Result<Pronunciation> Create(
        string ipa,
        Accent accent,
        string audioUrl,
        AudioSource audioSource)
    {
        if (string.IsNullOrWhiteSpace(ipa))
            return Result.Failure<Pronunciation>("IPA must not be empty.");

        if (string.IsNullOrWhiteSpace(audioUrl))
            return Result.Failure<Pronunciation>("Audio URL must not be empty.");

        return new Pronunciation(
            ipa.Trim(),
            accent,
            audioUrl.Trim(),
            audioSource);
    }
}
