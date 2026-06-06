using AngleSharp;
using AngleSharp.Dom;
using AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.RegularExpressions;

namespace AnkiBridge.Infrastructure.Services.Scraping;

/// <summary>
/// Scrapes Cambridge Dictionary (https://dictionary.cambridge.org) using AngleSharp.
/// HTTP is handled by <see cref="IBrowsingContext"/> (configured in DI with a
/// custom HttpClientRequester carrying the required User-Agent header).
///
/// Cambridge HTML structure targeted:
///   .entry-body__el / .idiom-block     → one block per POS / idiom
///     .hw.dhw                           → headword
///     .pos.dpos                         → part of speech
///     .uk.dpron-i / .us.dpron-i         → pronunciation blocks
///       .ipa.dipa                       → IPA string
///       source[type=audio/mpeg]         → audio URL
///     .def-block                        → one block per definition
///       .def.ddef_d                     → definition text
///       .examp .eg                      → example sentences
/// </summary>
public sealed class CambridgeDictionaryProvider(
    IOptions<CambridgeDictionaryOptions> options,
    ILogger<CambridgeDictionaryProvider> logger)
    : IDictionaryProvider, IDisposable
{
    /// <summary>
    /// Selectors tried in priority order. The first one that yields at least one element wins;
    /// remaining selectors are skipped. Prefer CALD4 over CACD, entry blocks over idiom blocks.
    /// </summary>
    private static readonly string[] EntryBlockSelectors =
    [
        ".dictionary[data-id='cald4'] .entry-body__el",
        ".dictionary[data-id='cacd'] .entry-body__el",
        ".dictionary[data-id='cald4'] .idiom-block",
        ".dictionary[data-id='cacd'] .idiom-block",
    ];

    private readonly CambridgeDictionaryOptions _options = options.Value;
    private readonly IBrowsingContext _context = BrowsingContext.New(
        Configuration.Default.WithDefaultLoader());

    // ── Public API ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ScrapedEntry>>> ScrapeAsync(
        string word,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(word);

        var url = BuildUrl(word);
        logger.LogDebug("Fetching Cambridge page: {Url}", url);

        IDocument document;
        try
        {
            document = await _context.OpenAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open Cambridge page: {Url}", url);
            return Result.Failure<IReadOnlyList<ScrapedEntry>>("Cambridge Dictionary is unavailable.");
        }

        return document.StatusCode switch
        {
            HttpStatusCode.NotFound => OnNotFound(word),
            _ when (int)document.StatusCode >= 400 => OnHttpError(word, document.StatusCode),
            _ => ParseDocument(document, url, word),
        };
    }

    public void Dispose() => _context.Dispose();

    // ── HTTP status handlers ─────────────────────────────────────────────────

    private Result<IReadOnlyList<ScrapedEntry>> OnNotFound(string word)
    {
        logger.LogDebug("Cambridge 404 — word not found: {Word}", word);
        return Result.Success<IReadOnlyList<ScrapedEntry>>([]);
    }

    private Result<IReadOnlyList<ScrapedEntry>> OnHttpError(string word, HttpStatusCode status)
    {
        logger.LogError("Cambridge returned {Status} for word={Word}", (int)status, word);
        return Result.Failure<IReadOnlyList<ScrapedEntry>>("Cambridge Dictionary scraping failed.");
    }

    // ── Parse ────────────────────────────────────────────────────────────────

    private Result<IReadOnlyList<ScrapedEntry>> ParseDocument(
        IDocument document, string url, string word)
    {
        try
        {
            var blocks = FindEntryBlocks(document);

            if (blocks.Length == 0)
            {
                logger.LogWarning("No entry blocks found for word={Word}", word);
                return Result.Success<IReadOnlyList<ScrapedEntry>>([]);
            }

            var entries = blocks
                .Select(block => ParseEntryBlock(block, url))
                .OfType<ScrapedEntry>()
                .ToList()
                .AsReadOnly();

            logger.LogInformation("Scraped {Count} entries for word={Word}", entries.Count, word);
            return Result.Success<IReadOnlyList<ScrapedEntry>>(entries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Cambridge HTML for word={Word}", word);
            return Result.Failure<IReadOnlyList<ScrapedEntry>>("Failed to parse Cambridge Dictionary response.");
        }
    }

    /// <summary>
    /// Iterates <see cref="EntryBlockSelectors"/> in order and returns the first
    /// non-empty result. Returns an empty array when no selector matches.
    /// </summary>
    private static IElement[] FindEntryBlocks(IDocument document)
    {
        foreach (var selector in EntryBlockSelectors)
        {
            var blocks = document.QuerySelectorAll(selector);
            if (blocks.Length > 0)
                return [.. blocks];
        }

        return [];
    }

    private ScrapedEntry? ParseEntryBlock(IElement block, string sourceUrl)
    {
        var headword = block.QuerySelector(".hw.dhw")?.TextContent.Trim();
        if (string.IsNullOrWhiteSpace(headword))
        {
            logger.LogDebug("Skipping entry block — no headword found");
            return null;
        }

        return new ScrapedEntry(
            Headword: headword,
            PartOfSpeech: block.QuerySelector(".pos.dpos")?.TextContent.Trim() ?? string.Empty,
            SourceUrl: sourceUrl,
            Pronunciations: ParsePronunciations(block),
            Definitions: ParseDefinitions(block));
    }

    private static List<ScrapedPronunciation> ParsePronunciations(IElement block)
    {
        var result = new List<ScrapedPronunciation>(2);
        TryAddPronunciation(block, ".uk.dpron-i", Accent.British, result);
        TryAddPronunciation(block, ".us.dpron-i", Accent.American, result);
        return result;
    }

    private static void TryAddPronunciation(
        IElement block, string selector, Accent accent, List<ScrapedPronunciation> target)
    {
        var pronBlock = block.QuerySelector(selector);
        if (pronBlock is null) return;

        var ipa = pronBlock.QuerySelector(".ipa.dipa")?.TextContent.Trim();
        if (string.IsNullOrWhiteSpace(ipa)) return;

        var audioSrc = pronBlock
            .QuerySelectorAll("source")
            .FirstOrDefault(s => s.GetAttribute("type") == "audio/mpeg")
            ?.GetAttribute("src");

        target.Add(new ScrapedPronunciation(accent, ipa, audioSrc is not null
            ? ToAbsoluteAudioUrl(audioSrc)
            : null));
    }

    private static List<ScrapedDefinition> ParseDefinitions(IElement block) =>
        block.QuerySelectorAll(".def-block")
             .Select(ParseDefinitionBlock)
             .OfType<ScrapedDefinition>()
             .ToList();

    private static ScrapedDefinition? ParseDefinitionBlock(IElement defBlock)
    {
        var text = defBlock.QuerySelector(".def.ddef_d")?.TextContent;
        if (string.IsNullOrWhiteSpace(text)) return null;

        var examples = defBlock
            .QuerySelectorAll(".examp .eg")
            .Select(e => e.TextContent.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList()
            .AsReadOnly();

        var definition = CollapseWhitespace(text).TrimEnd(':').TrimEnd();

        return new ScrapedDefinition(definition, examples);
    }

    // ── Utilities ────────────────────────────────────────────────────────────

    private string BuildUrl(string word)
    {
        var slug = word.Trim().ToLowerInvariant().Replace(' ', '-');
        return $"{_options.BaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(slug)}";
    }

    private static string ToAbsoluteAudioUrl(string src) =>
        src.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? src
            : $"https://dictionary.cambridge.org{src}";

    private static string CollapseWhitespace(string input) =>
        Regex.Replace(input.Trim(), @"\s+", " ");
}