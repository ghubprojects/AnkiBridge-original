using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;

public sealed record DictionaryEntryDetailTranslation(
    string Text,
    TranslationSource Source
);
