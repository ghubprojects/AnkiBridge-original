namespace AnkiBridge.Infrastructure.Services.Scraping;

/// <summary>
/// Configuration for <see cref="CambridgeDictionaryProvider"/>.
/// Bind from appsettings section <c>Dictionary:Cambridge</c>.
/// </summary>
public sealed class CambridgeDictionaryOptions
{
    public const string SectionName = "Dictionary:Cambridge";

    /// <summary>
    /// Base URL of Cambridge Dictionary. Default is the English dictionary.
    /// </summary>
    public string BaseUrl { get; set; } = "https://dictionary.cambridge.org/dictionary/english";

    /// <summary>
    /// HTTP request timeout. Defaults to 15 seconds.
    /// Cambridge pages can be slow under load.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}
