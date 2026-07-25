namespace AnkiBridge.Infrastructure.ExternalServices.AnkiConnect.Contracts;

public sealed record AnkiConnectRequest(
    string Action,
    object Params,
    int Version);