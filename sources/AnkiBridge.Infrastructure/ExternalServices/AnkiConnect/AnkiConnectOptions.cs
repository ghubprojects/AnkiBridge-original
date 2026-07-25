namespace AnkiBridge.Infrastructure.ExternalServices.AnkiConnect;

public sealed class AnkiConnectOptions
{
    public const string SectionName = "AnkiConnect";

    public string BaseUrl { get; set; } = "http://localhost:8765";
    public int TimeoutSeconds { get; set; } = 10;
}
