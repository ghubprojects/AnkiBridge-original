namespace AnkiBridge.Application.Common.Contracts.Images;

/// <summary>
/// Port for searching stock images from an external provider.
/// Implementations are responsible for a single provider (Pixabay, Pexels, …).
/// The composite implementation handles fallback across providers.
/// </summary>
public interface IImageProvider
{
    /// <summary>
    /// Searches for images matching <paramref name="query"/>.
    /// </summary>
    /// <param name="query">Search term — typically the headword or phrase.</param>
    /// <param name="count">Number of images to return. Default is 3 (initial load); pass multiples of 3 for "load more".</param>
    /// <param name="page">1-based page number. Used to implement "load more" without re-fetching earlier results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="ImageResult"/>.
    /// Returns an empty list — never throws — when no results are found.
    /// Throws on unrecoverable errors (network failure, invalid API key, …).
    /// </returns>
    Task<IReadOnlyList<ImageResult>> SearchAsync(
        string query,
        int count = 3,
        int page = 1,
        CancellationToken cancellationToken = default);
}