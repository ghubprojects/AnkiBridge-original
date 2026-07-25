using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Abstractions.Dictionary;

public sealed record DictionaryEntryResult(
    string Headword,
    PartOfSpeech PartOfSpeech,
    DictionarySource Source,
    IReadOnlyList<DictionaryDefinitionResult> Definitions,
    IReadOnlyList<DictionaryPronunciationResult> Pronunciations);
