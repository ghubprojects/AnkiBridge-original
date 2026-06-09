using AngleSharp;
using AngleSharp.Dom;
using AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AnkiBridge.Infrastructure.Services.Scraping;

public sealed class CambridgeTranslationProvider(
    IOptions<CambridgeDictionaryOptions> options,
    ILogger<CambridgeTranslationProvider> logger)
    : ITranslationProvider, IDisposable
{
    // Cambridge EV dùng cùng cấu trúc HTML với English dictionary
    // nhưng thêm block .trans chứa bản dịch tiếng Việt
    private static readonly string[] EntryBlockSelectors =
    [
        ".dictionary[data-id='cenv'] .entry-body__el",
        ".dictionary[data-id='cenv'] .idiom-block",
    ];

    private static readonly string[] DefinitionBlockSelectors =
        [".dsense_b > .ddef_block", ".sense-body > .def-block"];

    private static readonly string[] DefinitionSelectors = [".ddef_d"];

    // Cambridge EV-specific: bản dịch nằm trong .trans hoặc .dtrans
    private static readonly string[] TranslationSelectors = [".dtrans", ".trans"];

    private readonly IBrowsingContext _context = BrowsingContext.New(
        Configuration.Default.WithDefaultLoader());

    public async Task<Result<IReadOnlyList<TranslationLookupResult>>> GetTranslationsAsync(
        string word,
        CancellationToken ct = default)
    {
        var url = BuildUrl(word);
        IDocument document;

        try
        {
            document = await _context.OpenAsync(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open Cambridge EV page: {Url}", url);
            return Result.Failure<IReadOnlyList<TranslationLookupResult>>("Cambridge EV unavailable.");
        }

        if (document.StatusCode == HttpStatusCode.NotFound)
            return Result.Success<IReadOnlyList<TranslationLookupResult>>([]);

        if ((int)document.StatusCode >= 400)
            return Result.Failure<IReadOnlyList<TranslationLookupResult>>("Cambridge EV scraping failed.");

        return ParseTranslations(document);
    }

    private Result<IReadOnlyList<TranslationLookupResult>> ParseTranslations(IDocument document)
    {
        var results = new List<TranslationLookupResult>();

        foreach (var selector in EntryBlockSelectors)
        {
            var blocks = document.QuerySelectorAll(selector);
            if (blocks.Length == 0) continue;

            foreach (var block in blocks)
            {
                var defBlocks = QueryFirstAll(block, DefinitionBlockSelectors);

                foreach (var defBlock in defBlocks)
                {
                    var englishDef = QueryFirstText(defBlock, DefinitionSelectors);
                    var translation = QueryFirstText(defBlock, TranslationSelectors);

                    if (string.IsNullOrWhiteSpace(englishDef) ||
                        string.IsNullOrWhiteSpace(translation))
                        continue;

                    results.Add(new TranslationLookupResult(
                        Text: translation.Trim(),
                        Source: TranslationSource.Cambridge));
                }
            }

            if (results.Count > 0) break; // first matching selector wins
        }

        return Result.Success<IReadOnlyList<TranslationLookupResult>>(results);
    }

    private static string? QueryFirstText(IElement root, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var text = root.QuerySelector(selector)?.TextContent.Trim();
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        return null;
    }

    private static IEnumerable<IElement> QueryFirstAll(IElement root, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var elements = root.QuerySelectorAll(selector);
            if (elements.Length > 0) return elements;
        }
        return [];
    }

    private string BuildUrl(string word)
    {
        var slug = word.Trim().ToLowerInvariant().Replace(' ', '-');
        return $"{options.Value.BaseUrl.TrimEnd('/')}/english-vietnamese/{Uri.EscapeDataString(slug)}";
    }

    public void Dispose() => _context.Dispose();
}