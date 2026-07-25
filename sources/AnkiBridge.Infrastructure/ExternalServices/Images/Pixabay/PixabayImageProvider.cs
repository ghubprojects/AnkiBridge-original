using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnkiBridge.Application.Abstractions.Images;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.ExternalServices.Images.Pixabay;

public sealed class PixabayImageProvider(HttpClient httpClient, IOptions<PixabayOptions> options) : IImageProvider
{
    private readonly PixabayOptions _options = options.Value;

    public async Task<Result<IReadOnlyList<ImageResult>>> SearchAsync(
        string keyword,
        int count = 3,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Result.Failure<IReadOnlyList<ImageResult>>("Keyword must not be empty.");

        PixabayResponse? response;

        try
        {
            var url = BuildRequestUrl(keyword, count, page);
            response = await httpClient.GetFromJsonAsync<PixabayResponse>(url, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<IReadOnlyList<ImageResult>>("Pixabay is unavailable.");
        }

        if (response?.Hits is not { Count: > 0 } hits)
            return Result.Failure<IReadOnlyList<ImageResult>>("No images found.");

        return hits
            .Select(h => new ImageResult(
                PreviewUrl: h.PreviewUrl,
                FullUrl: h.LargeImageUrl,
                Source: ImageSource.Pixabay))
            .ToList()
            .AsReadOnly();
    }

    private string BuildRequestUrl(string query, int count, int page) =>
        $"{_options.BaseUrl.TrimEnd('/')}" +
        $"?key={Uri.EscapeDataString(_options.ApiKey)}" +
        $"&q={Uri.EscapeDataString(query.Trim())}" +
        $"&image_type={_options.ImageType}" +
        $"&lang={_options.Language}" +
        $"&safesearch={_options.SafeSearch.ToString().ToLowerInvariant()}" +
        $"&per_page={count}" +
        $"&page={page}";

    #region Internal response models

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

    #endregion
}
