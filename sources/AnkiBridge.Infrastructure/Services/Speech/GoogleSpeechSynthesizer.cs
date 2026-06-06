using AnkiBridge.Application.Common.Contracts.Speech;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnkiBridge.Infrastructure.Services.Speech;

/// <summary>
/// Builds a Google Translate TTS audio URL.
/// No HTTP call is made — the URL is returned as-is and played by the browser.
/// Used as fallback when Cambridge Dictionary does not provide audio
/// (e.g. multi-word phrases, informal expressions).
/// </summary>
public sealed class GoogleSpeechSynthesizer(IOptions<GoogleSpeechOptions> options) : ISpeechSynthesizer
{
    // Google Translate TTS endpoint — stable but unofficial; no auth required with tw-ob client.
    private const string BaseUrl = "https://translate.google.com/translate_tts";

    private readonly GoogleSpeechOptions _options = options.Value;

    /// <inheritdoc/>
    public string BuildAudioUrl(string text, string language = "en")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var lang = string.IsNullOrWhiteSpace(language) ? _options.DefaultLanguage : language;
        var encoded = Uri.EscapeDataString(text.Trim());

        return $"{BaseUrl}?ie=UTF-8&q={encoded}&tl={lang}&client={_options.Client}";
    }
}