namespace AnkiBridge.Application.Common.Contracts.Images;

/// <summary>
/// Represents a single image result returned by any image provider.
/// </summary>
/// <param name="ThumbnailUrl">Lower-resolution URL suitable for gallery display.</param>
/// <param name="FullUrl">Full-resolution URL used when user selects the image.</param>
/// <param name="AltText">Descriptive text for accessibility and display; may be null.</param>
/// <param name="Provider">Identifies which provider returned this result (e.g. "Pixabay", "Pexels").</param>
public sealed record ImageResult(
    string ThumbnailUrl,
    string FullUrl,
    string? AltText,
    string Provider);
