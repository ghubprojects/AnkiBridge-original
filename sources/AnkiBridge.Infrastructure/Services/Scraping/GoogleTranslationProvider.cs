using AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AnkiBridge.Infrastructure.Services.Scraping;

/// <summary>
/// Uses the public Google Translate endpoint (no API key required).
/// Prefers dt=bd (bilingual dictionary — POS-grouped, multiple meanings);
/// falls back to dt=t (plain translation) when bd is absent.
/// </summary>
public sealed class GoogleTranslationProvider(
    HttpClient httpClient,
    ILogger<GoogleTranslationProvider> logger)
    : ITranslationProvider
{
    private const string BaseUrl =
        "https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=vi&dt=t&dt=bd&q=";

    public async Task<Result<IReadOnlyList<TranslationLookupResult>>> GetTranslationsAsync(
        string word,
        CancellationToken ct = default)
    {
        var url = BaseUrl + Uri.EscapeDataString(word);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Translate request failed for word={Word}", word);
            return Result.Failure<IReadOnlyList<TranslationLookupResult>>("Google Translate unavailable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Google Translate returned {Status} for word={Word}",
                (int)response.StatusCode, word);
            return Result.Failure<IReadOnlyList<TranslationLookupResult>>(
                $"Google Translate returned {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseResponse(json, word);
    }

    // ── Parsing ───────────────────────────────────────────────────────────────
    //
    // Google returns a jagged JSON array.  With dt=t and dt=bd the shape is:
    //
    //   [
    //     [["translation","original",...], ...],          ← [0] dt=t
    //     [["pos", [["vi_word",...], ...], null, "en"],   ← [1] dt=bd
    //      ...],
    //     ...
    //   ]
    //
    // dt=bd is preferred: it groups by POS and lists multiple meanings.
    // dt=t is used as fallback when dt=bd is absent or empty.

    private Result<IReadOnlyList<TranslationLookupResult>> ParseResponse(string json, string word)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                return Empty();

            var results = new List<TranslationLookupResult>();

            // dt=bd at index [1]
            if (root.GetArrayLength() > 1)
            {
                var bd = root[1];
                if (bd.ValueKind == JsonValueKind.Array)
                {
                    foreach (var posGroup in bd.EnumerateArray())
                    {
                        // posGroup = ["pos", [["vi_word", ...], ...], null, "en_word"]
                        if (posGroup.ValueKind != JsonValueKind.Array || posGroup.GetArrayLength() < 2)
                            continue;

                        var entries = posGroup[1];
                        if (entries.ValueKind != JsonValueKind.Array) continue;

                        foreach (var entry in entries.EnumerateArray())
                        {
                            if (entry.ValueKind == JsonValueKind.Array && entry.GetArrayLength() > 0)
                                TryAdd(results, entry[0].GetString(), TranslationSource.Google);
                        }
                    }
                }
            }

            // dt=t fallback at index [0]
            if (results.Count == 0 && root.GetArrayLength() > 0)
            {
                var t = root[0];
                if (t.ValueKind == JsonValueKind.Array)
                    foreach (var segment in t.EnumerateArray())
                        if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0)
                            TryAdd(results, segment[0].GetString(), TranslationSource.Google);
            }

            return Result.Success<IReadOnlyList<TranslationLookupResult>>(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Google Translate response for word={Word}", word);
            return Result.Failure<IReadOnlyList<TranslationLookupResult>>(
                "Failed to parse Google Translate response.");
        }
    }

    private static void TryAdd(List<TranslationLookupResult> list, string? text, TranslationSource source)
    {
        if (!string.IsNullOrWhiteSpace(text))
            list.Add(new TranslationLookupResult(text.Trim(), source));
    }

    private static Result<IReadOnlyList<TranslationLookupResult>> Empty() =>
        Result.Success<IReadOnlyList<TranslationLookupResult>>([]);
}