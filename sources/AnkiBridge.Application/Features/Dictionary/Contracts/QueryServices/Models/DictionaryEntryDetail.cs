using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;

public sealed record DictionaryEntryDetail(
    Guid Id,
    string Headword,
    PartOfSpeech PartOfSpeech,
    DictionarySource Source,
    List<DictionaryEntryDetailDefinition> Definitions,
    List<DictionaryEntryDetailTranslation> Translations,
    List<DictionaryEntryDetailPronunciation> Pronunciations,
    List<DictionaryEntryDetailImage> Images
);