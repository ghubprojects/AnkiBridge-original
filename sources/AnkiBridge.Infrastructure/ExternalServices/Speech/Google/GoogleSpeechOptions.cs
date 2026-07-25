namespace AnkiBridge.Infrastructure.ExternalServices.Speech.Google;

/// <summary>
/// Configuration for <see cref="GoogleSpeechProvider"/>.
/// Bind from appsettings section <c>Speech:Google</c>.
/// </summary>
public sealed class GoogleSpeechOptions
{
    public const string SectionName = "Speech:Google";

    /// <summary>
    /// The Google Translate TTS endpoint URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://translate.google.com/translate_tts";

    /// <summary>
    /// BCP-47 language tag for speech synthesis.
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// The <c>client</c> query-string parameter sent to the Google TTS endpoint.
    /// Defaults to <c>tw-ob</c> for lightweight usage without an API key.
    /// </summary>
    public string Client { get; set; } = "tw-ob";
}
