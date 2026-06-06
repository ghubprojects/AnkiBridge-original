namespace AnkiBridge.Application.Common.Contracts.Speech;

/// <summary>
/// Port for generating an audio URL from text.
/// The returned URL is stored as <c>AudioUrl</c> on a <c>LearningEntry</c>
/// and played directly by the browser — no audio is downloaded server-side.
/// </summary>
public interface ISpeechSynthesizer
{
    /// <summary>
    /// Builds a public audio URL for the given <paramref name="text"/>.
    /// </summary>
    /// <param name="text">Word or phrase to synthesize.</param>
    /// <param name="language">BCP-47 language tag (e.g. "en", "en-US"). Defaults to English.</param>
    /// <returns>A URL string the browser can use as an <c>&lt;audio&gt;</c> source.</returns>
    string BuildAudioUrl(string text, string language = "en");
}
