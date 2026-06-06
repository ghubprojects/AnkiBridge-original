using AnkiBridge.Application.Common.Contracts.Images;
using Microsoft.Extensions.Logging;

namespace AnkiBridge.Infrastructure.Services.Images;

/// <summary>
/// Composite <see cref="IImageProvider"/> that delegates to a primary provider (Pixabay)
/// and falls back to a secondary provider (Pexels) when the primary is unavailable or returns no results.
///
/// This is the implementation registered against <see cref="IImageProvider"/> in DI.
/// Application layer only knows about <see cref="IImageProvider"/> and is unaware of this composite.
/// </summary>
public sealed class CompositeImageProvider : IImageProvider
{
    private readonly PixabayImageProvider _primary;
    private readonly PexelsImageProvider _secondary;
    private readonly ILogger<CompositeImageProvider> _logger;

    public CompositeImageProvider(
        PixabayImageProvider primary,
        PexelsImageProvider secondary,
        ILogger<CompositeImageProvider> logger)
    {
        _primary = primary;
        _secondary = secondary;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Strategy:
    /// 1. Try primary (Pixabay). If it returns ≥1 result → return immediately.
    /// 2. If primary throws or returns empty → warn and try secondary (Pexels).
    /// 3. If secondary also fails → propagate the secondary exception.
    /// </remarks>
    public async Task<IReadOnlyList<ImageResult>> SearchAsync(
        string query,
        int count = 3,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var primaryResults = await _primary.SearchAsync(query, count, page, cancellationToken);

            if (primaryResults.Count > 0)
                return primaryResults;

            _logger.LogWarning(
                "Pixabay returned 0 results for query={Query}. Falling back to Pexels.", query);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Pixabay failed for query={Query}. Falling back to Pexels.", query);
        }

        return await _secondary.SearchAsync(query, count, page, cancellationToken);
    }
}
