namespace AnkiBridge.Infrastructure.IntegrationTests.Common;

/// <summary>
/// Central place for xUnit <c>[Trait("Category", ...)]</c> values, so category names are never
/// hand-typed (and never typo'd) at each call site. Use with <c>dotnet test --filter</c>, e.g.
/// <c>dotnet test --filter Category!=Integration</c> to skip network-dependent tests.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Tests that hit a real, external network destination — no <see cref="System.Net.Http.HttpMessageHandler"/>
    /// is stubbed. These are slower and can be flaky without network access, so CI should run
    /// them on a separate, scheduled lane rather than gating every PR on them.
    /// </summary>
    public const string Integration = "Integration";
}
