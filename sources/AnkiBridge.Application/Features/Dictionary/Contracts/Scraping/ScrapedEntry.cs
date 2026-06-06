using AnkiBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;

/// <summary>
/// Raw data scraped from an external dictionary source.
/// Represents one POS block (e.g. "apple" as noun, "run" as verb).
/// One word can produce multiple <see cref="ScrapedEntry"/> instances.
/// </summary>
public sealed record ScrapedEntry(
    string Headword,
    string PartOfSpeech,
    string SourceUrl,
    IReadOnlyList<ScrapedPronunciation> Pronunciations,
    IReadOnlyList<ScrapedDefinition> Definitions);

public sealed record ScrapedPronunciation(
    Accent Accent,
    string Ipa,

    /// <summary>
    /// Absolute audio URL from Cambridge (mp3).
    /// Null when Cambridge does not provide audio for this accent/entry.
    /// </summary>
    string? AudioUrl);

public sealed record ScrapedDefinition(
    string Text,
    IReadOnlyList<string> Examples);
