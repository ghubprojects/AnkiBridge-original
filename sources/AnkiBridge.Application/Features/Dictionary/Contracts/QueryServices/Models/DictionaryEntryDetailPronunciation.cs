using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;

public sealed record DictionaryEntryDetailPronunciation(
    string Ipa,
    Accent Accent,
    string? AudioUrl,
    AudioSource? AudioSource
);
