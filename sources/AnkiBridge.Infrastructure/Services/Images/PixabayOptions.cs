namespace AnkiBridge.Infrastructure.Services.Images;

/// <summary>
/// Configuration for <see cref="PixabayImageProvider"/>.
/// Bind from appsettings section <c>Images:Pixabay</c>.
/// </summary>
public sealed class PixabayOptions
{
    public const string SectionName = "Images:Pixabay";

    /// <summary>
    /// Pixabay API key. Required. Obtain at https://pixabay.com/api/docs/
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Accepted image types: "all", "photo", "illustration", "vector".
    /// Defaults to "photo" for vocabulary flashcards.
    /// </summary>
    public string ImageType { get; set; } = "photo";

    /// <summary>
    /// Restrict results to a specific language for the search term.
    /// Uses Pixabay language codes (e.g. "en"). Defaults to "en".
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Whether to return only "safe search" images. Defaults to true.
    /// </summary>
    public bool SafeSearch { get; set; } = true;
}