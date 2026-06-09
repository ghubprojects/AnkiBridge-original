using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;

/// <summary>
/// Port for fetching dictionary data from an external source.
/// All outcomes — including "not found" and network errors — are expressed
/// as <see cref="Result{T}"/> values; the caller never needs to catch exceptions
/// for expected failure scenarios.
/// </summary>
public interface IDictionaryProvider
{
    /// <summary>
    /// Fetches all POS entries for the given word or phrase.
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    ///   <item><c>Success</c> with an empty list — word not found on the provider.</item>
    ///   <item><c>Success</c> with one or more entries — normal case.</item>
    ///   <item><c>Failure</c> (<see cref="ErrorType.Failure"/>) — network or parse error.</item>
    /// </list>
    /// </returns>
    Task<Result<IReadOnlyList<ScrapedEntry>>> ScrapeAsync(
        string word,
        CancellationToken cancellationToken = default);
}

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