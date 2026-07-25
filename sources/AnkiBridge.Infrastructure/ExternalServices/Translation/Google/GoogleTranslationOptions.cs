namespace AnkiBridge.Infrastructure.ExternalServices.Translation.Google;

/// <summary>
/// Configuration for <see cref="GoogleTranslationProvider"/>.
/// Bind from appsettings section <c>Translation:Google</c>.
/// </summary>
public sealed class GoogleTranslationOptions
{
    public const string SectionName = "Translation:Google";

    /// <summary>
    /// The Google Translate API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://translate.googleapis.com";

    /// <summary>
    /// BCP-47 language tag for the source text.
    /// </summary>
    public string SourceLanguage { get; set; } = "en";

    /// <summary>
    /// BCP-47 language tag for the translated text.
    /// </summary>
    public string TargetLanguage { get; set; } = "vi";

    /// <summary>
    /// Timeout for translation requests, in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
