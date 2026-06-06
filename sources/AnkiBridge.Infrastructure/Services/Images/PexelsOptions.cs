namespace AnkiBridge.Infrastructure.Services.Images;

/// <summary>
/// Configuration for <see cref="PexelsImageProvider"/>.
/// Bind from appsettings section <c>Images:Pexels</c>.
/// </summary>
public sealed class PexelsOptions
{
    public const string SectionName = "Images:Pexels";

    /// <summary>
    /// Pexels API key. Required. Obtain at https://www.pexels.com/api/
    /// Free tier: 200 req/hour, 20,000 req/month. No attribution required in UI
    /// (but API terms require a link back to Pexels on the photo page).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Locale for results. Uses IETF language tags (e.g. "en-US"). Defaults to "en-US".
    /// </summary>
    public string Locale { get; set; } = "en-US";

    /// <summary>
    /// Image orientation filter: "landscape", "portrait", "square", or empty for any.
    /// </summary>
    public string Orientation { get; set; } = string.Empty;
}
