using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Abstractions.Translation;

public sealed record TranslationResult(string Text, TranslationSource Source);
