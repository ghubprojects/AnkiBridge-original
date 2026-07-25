namespace AnkiBridge.Infrastructure.ExternalServices.AnkiConnect.Contracts;

public sealed record AnkiConnectResponse<T>(
    T? Result,
    string? Error
);