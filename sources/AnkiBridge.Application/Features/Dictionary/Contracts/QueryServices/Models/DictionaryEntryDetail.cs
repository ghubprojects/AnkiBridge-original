using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;

public sealed record DictionaryEntryDetail(
    Guid Id,
    string Headword,
    PartOfSpeech PartOfSpeech,
    DictionarySource Source,
    IReadOnlyList<DictionaryEntryDetailDefinition> Definitions,
    IReadOnlyList<DictionaryEntryDetailTranslation> Translations,
    IReadOnlyList<DictionaryEntryDetailPronunciation> Pronunciations,
    IReadOnlyList<DictionaryEntryDetailImage> Images
);
