using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Infrastructure.Persistence.Seeding.Models;

internal sealed record DictionaryEntrySeed
{
    public required string Headword { get; init; }
    public PartOfSpeech PartOfSpeech { get; init; }
    public DictionarySource Source { get; init; }

    public List<DictionaryPronunciationSeed> Pronunciations { get; init; } = [];
    public List<DictionaryDefinitionSeed> Definitions { get; init; } = [];
    public List<DictionaryImageSeed> Images { get; init; } = [];
}

internal sealed record DictionaryPronunciationSeed
{
    public required string Ipa { get; init; }
    public Accent Accent { get; init; }
    public required string AudioUrl { get; init; }
    public AudioSource AudioSource { get; init; }
}

internal sealed record DictionaryDefinitionSeed
{
    public required string Text { get; init; }
    public List<string> Examples { get; init; } = [];
}

internal sealed record DictionaryImageSeed
{
    public required string Url { get; init; }
    public ImageSource Source { get; init; }
}
