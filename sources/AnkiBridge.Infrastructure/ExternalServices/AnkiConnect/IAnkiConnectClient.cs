using AnkiBridge.Shared.Results;

namespace AnkiBridge.Infrastructure.ExternalServices.AnkiConnect;

public interface IAnkiConnectClient
{
    Task<Result<T>> SendAsync<T>(
       string action,
       object? parameters,
       CancellationToken cancellationToken);
}