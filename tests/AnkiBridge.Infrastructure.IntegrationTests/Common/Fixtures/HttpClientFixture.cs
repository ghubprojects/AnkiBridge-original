namespace AnkiBridge.Infrastructure.IntegrationTests.Common.Fixtures;

/// <summary>
/// Shares a single real <see cref="HttpClient"/> across every test in a class that implements
/// <c>IClassFixture&lt;HttpClientFixture&gt;</c>, avoiding the well-known socket-exhaustion
/// pitfall of constructing a new <see cref="HttpClient"/> per test/request.
///
/// Tests that need non-default settings (e.g. a very short <see cref="HttpClient.Timeout"/> to
/// provoke a real timeout) should NOT reuse this shared instance — mutating it would leak the
/// setting into every other test in the class. Create a short-lived, locally scoped
/// <see cref="HttpClient"/> for those cases instead.
/// </summary>
public sealed class HttpClientFixture : IDisposable
{
    public HttpClient HttpClient { get; } = new();

    public void Dispose() => HttpClient.Dispose();
}
