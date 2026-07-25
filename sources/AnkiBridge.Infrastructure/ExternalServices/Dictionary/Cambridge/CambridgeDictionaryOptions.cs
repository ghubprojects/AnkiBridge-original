namespace AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;

/// <summary>
/// Configuration for Cambridge Dictionary providers.
/// Bind from appsettings section <c>Dictionary:Cambridge</c>.
/// </summary>
public sealed class CambridgeDictionaryOptions
{
    public const string SectionName = "Dictionary:Cambridge";

    /// <summary>
    /// The Cambridge Dictionary base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://dictionary.cambridge.org/dictionary";

    /// <summary>
    /// Timeout for Cambridge Dictionary requests, in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}
