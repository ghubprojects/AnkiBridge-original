using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnkiBridge.Application.Abstractions.Images;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.ExternalServices.Images.Pexels;

public sealed class PexelsImageProvider(HttpClient http, IOptions<PexelsOptions> options) : IImageProvider
{
    private readonly PexelsOptions _options = options.Value;

    public async Task<Result<IReadOnlyList<ImageResult>>> SearchAsync(
        string keyword,
        int count = 3,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Result.Failure<IReadOnlyList<ImageResult>>("Keyword must not be empty.");

        PexelsResponse? response;

        try
        {
            var url = BuildRequestUrl(keyword, count, page);
            response = await http.GetFromJsonAsync<PexelsResponse>(url, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<IReadOnlyList<ImageResult>>("Pexels is unavailable.");
        }

        if (response?.Photos is not { Count: > 0 } photos)
            return Result.Failure<IReadOnlyList<ImageResult>>("No images found.");

        var results = photos
            .Select(p => new ImageResult(
                PreviewUrl: p.Src.Medium,
                FullUrl: p.Src.Large,
                Source: ImageSource.Pexels))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<ImageResult>>(results);
    }

    private string BuildRequestUrl(string query, int count, int page)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}" +
                  $"?query={Uri.EscapeDataString(query.Trim())}" +
                  $"&locale={Uri.EscapeDataString(_options.Locale)}" +
                  $"&per_page={count}" +
                  $"&page={page}";

        if (!string.IsNullOrWhiteSpace(_options.Orientation))
            url += $"&orientation={Uri.EscapeDataString(_options.Orientation.Trim())}";

        return url;
    }

    #region Internal response models

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
        public PexelsPhotoSource Src { get; init; } = new();
    }

    private sealed class PexelsPhotoSource
    {
        /// <summary>Approx 350px wide — used as thumbnail.</summary>
        [JsonPropertyName("medium")]
        public string Medium { get; init; } = string.Empty;

        /// <summary>Approx 1280px wide — used as full image.</summary>
        [JsonPropertyName("large")]
        public string Large { get; init; } = string.Empty;
    }

    #endregion
}
