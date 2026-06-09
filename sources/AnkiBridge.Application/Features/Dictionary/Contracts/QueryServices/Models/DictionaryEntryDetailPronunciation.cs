using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;

public sealed record DictionaryEntryDetailPronunciation(
    Accent Accent,
    string Ipa,
    AudioSource AudioSource,
    string AudioUrl
);