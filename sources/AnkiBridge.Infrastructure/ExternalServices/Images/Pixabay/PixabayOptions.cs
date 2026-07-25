namespace AnkiBridge.Infrastructure.ExternalServices.Images.Pixabay;

/// <summary>
/// Configuration for <see cref="PixabayImageProvider"/>.
/// Bind from appsettings section <c>Images:Pixabay</c>.
/// </summary>
public sealed class PixabayOptions
{
    public const string SectionName = "Images:Pixabay";

    /// <summary>
    /// The Pixabay API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://pixabay.com/api/";

    /// <summary>
    /// Pixabay API key. Obtain at https://pixabay.com/api/docs/
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Accepted image types: "all", "photo", "illustration", "vector".
    /// </summary>
    public string ImageType { get; set; } = "photo";

    /// <summary>
    /// Restrict results to a specific language for the search term.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Whether Pixabay safe-search filtering is enabled.
    /// </summary>
    public bool SafeSearch { get; set; } = true;

    /// <summary>
    /// Timeout for Pixabay requests, in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
