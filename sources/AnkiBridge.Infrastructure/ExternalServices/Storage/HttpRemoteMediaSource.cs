using AnkiBridge.Application.Common.Contracts.Storage;

namespace AnkiBridge.Infrastructure.ExternalServices.Storage;

public sealed class HttpRemoteMediaSource(IHttpClientFactory httpClientFactory)
    : IRemoteMediaSource
{
    public async Task<RemoteMediaContent> OpenReadAsync(
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("media-download");
        var response = await client.GetAsync(
            sourceUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            return new RemoteMediaContent(
                stream,
                response.Content.Headers.ContentType?.MediaType,
                response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
