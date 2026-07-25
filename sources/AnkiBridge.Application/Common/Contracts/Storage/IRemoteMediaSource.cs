namespace AnkiBridge.Application.Common.Contracts.Storage;

public interface IRemoteMediaSource
{
    Task<RemoteMediaContent> OpenReadAsync(
        string sourceUrl,
        CancellationToken cancellationToken = default);
}

public sealed class RemoteMediaContent(
    Stream stream,
    string? contentType,
    IDisposable owner) : IAsyncDisposable
{
    public Stream Stream { get; } = stream;
    public string? ContentType { get; } = contentType;

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
        owner.Dispose();
    }
}
