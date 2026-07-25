using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Abstractions.Dictionary;

public sealed record DictionaryPronunciationResult(
    Accent Accent,
    string Ipa,
    string? AudioUrl);
