using AnkiBridge.Application.Abstractions.Speech;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.ExternalServices.Speech.Google;

public sealed class GoogleSpeechProvider(IOptions<GoogleSpeechOptions> options) : IAudioProvider
{
    private readonly GoogleSpeechOptions _options = options.Value;

    public Result<AudioResult> Synthesize(string text, string languageCode = "en")
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<AudioResult>.Failure("Text must not be empty.");

        var lang = string.IsNullOrWhiteSpace(languageCode)
             ? _options.DefaultLanguage
             : languageCode;

        var encodedText = Uri.EscapeDataString(text.Trim());
        var url = $"{_options.BaseUrl}?ie=UTF-8&q={encodedText}&tl={lang}&client={_options.Client}";

        return new AudioResult(url, AudioSource.Google);
    }
}
