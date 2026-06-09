using AnkiBridge.Application.Common.Contracts.Images;
using AnkiBridge.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AnkiBridge.Infrastructure.Services.Images;

/// <summary>
/// Searches for images using the Pexels API.
/// Registered as a named <see cref="HttpClient"/> ("Pexels").
/// The API key is injected via the Authorization header in DI registration.
/// </summary>
public sealed class PexelsImageProvider(
    HttpClient http,
    IOptions<PexelsOptions> options,
    ILogger<PexelsImageProvider> logger) 
    : IImageProvider
{
    private const string ProviderName = "Pexels";
    private const string BaseUrl = "https://api.pexels.com/v1/search";
    private readonly PexelsOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ImageResult>> SearchAsync(
        string query,
        int count = 3,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var url = BuildRequestUrl(query, count, page);

        logger.LogDebug("Searching Pexels: query={Query} count={Count} page={Page}", query, count, page);

        var response = await http.GetFromJsonAsync<PexelsResponse>(url, cancellationToken);

        if (response?.Photos is not { Count: > 0 } photos)
        {
            logger.LogDebug("Pexels returned no results for query={Query}", query);
            return [];
        }

        return photos
            .Select(p => new ImageResult(
                PreviewUrl: p.Src.Medium,
                FullUrl: p.Src.Large,
                Source: ImageSource.Pexels))
            .ToList()
            .AsReadOnly();
    }

    private string BuildRequestUrl(string query, int count, int page)
    {
        var encoded = Uri.EscapeDataString(query.Trim());

        var url = $"{BaseUrl}?query={encoded}&per_page={count}&page={page}&locale={_options.Locale}";

        if (!string.IsNullOrWhiteSpace(_options.Orientation))
            url += $"&orientation={_options.Orientation}";

        return url;
    }

    // ── Internal response models ────────────────────────────────────────────

    private sealed class PexelsResponse
    {
        [JsonPropertyName("total_results")]
        public int TotalResults { get; init; }

        [JsonPropertyName("photos")]
        public List<PexelsPhoto> Photos { get; init; } = [];
    }

    private sealed class PexelsPhoto
    {
        [JsonPropertyName("alt")]
        public string Alt { get; init; } = string.Empty;

        [JsonPropertyName("src")]
        public PexelsPhotoSrc Src { get; init; } = new();
    }

    private sealed class PexelsPhotoSrc
    {
        /// <summary>Approx 350px wide — used as thumbnail.</summary>
        [JsonPropertyName("medium")]
        public string Medium { get; init; } = string.Empty;

        /// <summary>Approx 1280px wide — used as full image.</summary>
        [JsonPropertyName("large")]
        public string Large { get; init; } = string.Empty;
    }
}
