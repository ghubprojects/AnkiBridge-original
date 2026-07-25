using AnkiBridge.Application.Abstractions.Translation;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AnkiBridge.Infrastructure.ExternalServices.Translation.Google;

public sealed class GoogleTranslationProvider(
    HttpClient httpClient,
    IOptions<GoogleTranslationOptions> options)
    : ITranslationProvider
{
    private readonly GoogleTranslationOptions _options = options.Value;

    public async Task<Result<IReadOnlyList<TranslationResult>>> TranslateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<IReadOnlyList<TranslationResult>>("Text must not be empty.");

        var requestUri = BuildRequestUrl(text);
        string responseBody;

        try
        {
            responseBody = await httpClient.GetStringAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<IReadOnlyList<TranslationResult>>(
                $"Google Translate is unreachable ({ex.StatusCode?.ToString() ?? "network error"}).");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient's own timeout — distinct from the caller cancelling.
            // Genuine caller cancellation is left unhandled so it propagates normally.
            return Result.Failure<IReadOnlyList<TranslationResult>>("Google Translate request timed out.");
        }

        return ParseResponse(responseBody);
    }

    private static Result<IReadOnlyList<TranslationResult>> ParseResponse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return Result.Failure<IReadOnlyList<TranslationResult>>("Google Translate returned malformed JSON.");
        }

        using (document)
        {
            if (!TryExtractTranslatedText(document.RootElement, out var translatedText))
                return Result.Failure<IReadOnlyList<TranslationResult>>("Google Translate response had an unexpected shape.");

            if (string.IsNullOrWhiteSpace(translatedText))
                return Result.Failure<IReadOnlyList<TranslationResult>>("Google Translate returned an empty translation.");

            return new List<TranslationResult>
            {
                new(translatedText, TranslationSource.Google)
            };
        }
    }

    // dt=t response shape: [ [ [translatedChunk, originalChunk, ...], ... ], ... ]
    // Explicit ValueKind/length checks instead of try/catch around indexing —
    // an unexpected shape from this unofficial endpoint is an expected
    // possibility, not an exceptional condition, so it shouldn't drive control
    // flow via exceptions.
    private static bool TryExtractTranslatedText(JsonElement root, out string translatedText)
    {
        translatedText = string.Empty;

        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return false;

        var sentences = root[0];
        if (sentences.ValueKind != JsonValueKind.Array)
            return false;

        var sb = new StringBuilder();
        foreach (var sentence in sentences.EnumerateArray())
        {
            if (sentence.ValueKind != JsonValueKind.Array || sentence.GetArrayLength() == 0)
                continue;

            var chunk = sentence[0];
            if (chunk.ValueKind == JsonValueKind.String)
                sb.Append(chunk.GetString());
        }

        translatedText = sb.ToString().Trim();
        return true;
    }

    private string BuildRequestUrl(string query) =>
        $"{_options.BaseUrl.TrimEnd('/')}/translate_a/single" +
        $"?client=gtx&sl={Uri.EscapeDataString(_options.SourceLanguage)}" +
        $"&tl={Uri.EscapeDataString(_options.TargetLanguage)}" +
        $"&dt=t&q={Uri.EscapeDataString(query.Trim())}";
}
