using AnkiBridge.Application.Common.Contracts.Images;
using AnkiBridge.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AnkiBridge.Infrastructure.Services.Images;

/// <summary>
/// Searches for images using the Pixabay API.
/// Registered as a named <see cref="HttpClient"/> ("Pixabay").
/// </summary>
public sealed class PixabayImageProvider(
    HttpClient http,
    IOptions<PixabayOptions> options,
    ILogger<PixabayImageProvider> logger) 
    : IImageProvider
{
    private const string BaseUrl = "https://pixabay.com/api/";
    private readonly PixabayOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ImageResult>> SearchAsync(
        string query,
        int count = 3,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var url = BuildRequestUrl(query, count, page);

        logger.LogDebug("Searching Pixabay: query={Query} count={Count} page={Page}", query, count, page);

        var response = await http.GetFromJsonAsync<PixabayResponse>(url, cancellationToken);

        if (response?.Hits is not { Count: > 0 } hits)
        {
            logger.LogDebug("Pixabay returned no results for query={Query}", query);
            return [];
        }

        return hits
            .Select(h => new ImageResult(
                PreviewUrl: h.PreviewUrl,
                FullUrl: h.LargeImageUrl,
                Source: ImageSource.Pixabay))
            .ToList()
            .AsReadOnly();
    }

    private string BuildRequestUrl(string query, int count, int page)
    {
        var encoded = Uri.EscapeDataString(query.Trim());
        return $"{BaseUrl}?key={_options.ApiKey}"
             + $"&q={encoded}"
             + $"&image_type={_options.ImageType}"
             + $"&lang={_options.Language}"
             + $"&safesearch={_options.SafeSearch.ToString().ToLowerInvariant()}"
             + $"&per_page={count}"
             + $"&page={page}";
    }

    // ── Internal response models ────────────────────────────────────────────

    private sealed class PixabayResponse
    {
        [JsonPropertyName("totalHits")]
        public int TotalHits { get; init; }

        [JsonPropertyName("hits")]
        public List<PixabayHit> Hits { get; init; } = [];
    }

    private sealed class PixabayHit
    {
        [JsonPropertyName("previewURL")]
        public string PreviewUrl { get; init; } = string.Empty;

        [JsonPropertyName("largeImageURL")]
        public string LargeImageUrl { get; init; } = string.Empty;

        [JsonPropertyName("tags")]
        public string Tags { get; init; } = string.Empty;
    }
}