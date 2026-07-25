using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Abstractions.Speech;

public interface IAudioProvider
{
    Result<AudioResult> Synthesize(string text, string languageCode = "en");
}
