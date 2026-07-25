using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;

public sealed record DictionaryEntrySearchResult(
    Guid Id,
    string Headword,
    PartOfSpeech PartOfSpeech,
    DictionarySource Source
);
