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
///
/// Every element lookup uses an ordered fallback list: the first selector that finds
/// at least one element wins; the rest are skipped.
/// </summary>
public sealed class CambridgeDictionaryProvider(
    IOptions<CambridgeDictionaryOptions> options,
    ILogger<CambridgeDictionaryProvider> logger)
    : IDictionaryProvider, IDisposable
{
    // ── Selector priority lists ──────────────────────────────────────────────

    private static readonly string[] EntryBlockSelectors =
    [
        ".dictionary[data-id='cald4'] .entry-body__el",
        ".dictionary[data-id='cacd'] .entry-body__el",
        ".dictionary[data-id='cald4'] .idiom-block",
        ".dictionary[data-id='cacd'] .idiom-block",
    ];

    private static readonly string[] HeadwordSelectors = [".dhw", ".hw"];
    private static readonly string[] PartOfSpeechSelectors = [".dpos", ".pos"];
    private static readonly string[] UkContainerSelectors = [".uk.dpron-i", ".uk"];
    private static readonly string[] UsContainerSelectors = [".us.dpron-i", ".us"];
    private static readonly string[] IpaSelectors = [".dpron .dipa", ".pron .ipa"];
    private static readonly string[] DefinitionBlockSelectors = [".dsense_b > .ddef_block", ".sense-body > .def-block"];
    private static readonly string[] DefinitionSelectors = [".ddef_d"];
    private static readonly string[] ExampleSelectors = [".deg", ".eg"];

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

    private static IElement[] FindEntryBlocks(IDocument document)
    {
        foreach (var selector in EntryBlockSelectors)
        {
            var blocks = document.QuerySelectorAll(selector);
            if (blocks.Length > 0) return [.. blocks];
        }
        return [];
    }

    private ScrapedEntry? ParseEntryBlock(IElement block, string sourceUrl)
    {
        var headword = QueryFirstText(block, HeadwordSelectors);
        if (string.IsNullOrWhiteSpace(headword))
        {
            logger.LogDebug("Skipping entry block — no headword found");
            return null;
        }

        return new ScrapedEntry(
            Headword: headword,
            PartOfSpeech: QueryFirstText(block, PartOfSpeechSelectors) ?? string.Empty,
            SourceUrl: sourceUrl,
            Pronunciations: ParsePronunciations(block),
            Definitions: ParseDefinitions(block));
    }

    private static List<ScrapedPronunciation> ParsePronunciations(IElement block)
    {
        var result = new List<ScrapedPronunciation>(2);
        TryAddPronunciation(block, UkContainerSelectors, IpaSelectors, Accent.British, result);
        TryAddPronunciation(block, UsContainerSelectors, IpaSelectors, Accent.American, result);
        return result;
    }

    private static void TryAddPronunciation(
        IElement block,
        string[] containerSelectors,
        string[] ipaSelectors,
        Accent accent,
        List<ScrapedPronunciation> target)
    {
        var container = QueryFirst(block, containerSelectors);
        if (container is null) return;

        var ipa = QueryFirstText(container, ipaSelectors);
        if (string.IsNullOrWhiteSpace(ipa)) return;

        var audioSrc = container
            .QuerySelectorAll("source")
            .FirstOrDefault(s => s.GetAttribute("type") == "audio/mpeg")
            ?.GetAttribute("src");

        target.Add(new ScrapedPronunciation(accent, ipa, audioSrc is not null
            ? ToAbsoluteAudioUrl(audioSrc)
            : null));
    }

    private static List<ScrapedDefinition> ParseDefinitions(IElement block) =>
        QueryFirstAll(block, DefinitionBlockSelectors)
            .Select(ParseDefinitionBlock)
            .OfType<ScrapedDefinition>()
            .ToList();

    private static ScrapedDefinition? ParseDefinitionBlock(IElement defBlock)
    {
        var text = QueryFirstText(defBlock, DefinitionSelectors);
        if (string.IsNullOrWhiteSpace(text)) return null;

        var examples = QueryFirstAll(defBlock, ExampleSelectors)
            .Select(e => e.TextContent.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList()
            .AsReadOnly();

        return new ScrapedDefinition(
            CollapseWhitespace(text).TrimEnd(':').TrimEnd(),
            examples);
    }

    // ── Selector helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the first element matched by the first selector that yields a result.
    /// Returns <c>null</c> when no selector matches.
    /// </summary>
    private static IElement? QueryFirst(IElement root, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var el = root.QuerySelector(selector);
            if (el is not null) return el;
        }
        return null;
    }

    /// <summary>
    /// Returns the trimmed <see cref="IElement.TextContent"/> of the first element
    /// matched by the first selector that yields a non-empty result.
    /// Returns <c>null</c> when no selector matches.
    /// </summary>
    private static string? QueryFirstText(IElement root, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var text = root.QuerySelector(selector)?.TextContent.Trim();
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        return null;
    }

    /// <summary>
    /// Returns all elements matched by the first selector that yields at least one element;
    /// remaining selectors are skipped. Returns an empty enumerable when nothing matches.
    /// </summary>
    private static IEnumerable<IElement> QueryFirstAll(IElement root, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var elements = root.QuerySelectorAll(selector);
            if (elements.Length > 0) return elements;
        }
        return [];
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