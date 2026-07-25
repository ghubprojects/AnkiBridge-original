using System.Text;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Translation.Google;
using AnkiBridge.Infrastructure.IntegrationTests.Common;
using AnkiBridge.Infrastructure.IntegrationTests.Common.Fixtures;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.IntegrationTests.ExternalServices.Translation.Google;

/// <summary>
/// Genuine integration tests for <see cref="GoogleTranslationProvider"/>: every test here
/// exercises a real <see cref="HttpClient"/> against a real network destination — either the
/// live (unofficial) Google Translate endpoint, or an intentionally unreachable/slow one to
/// provoke real transport failures. No <see cref="HttpMessageHandler"/> is stubbed.
///
/// Tests that only need to verify JSON-parsing or error-mapping logic against canned response
/// bodies belong in a separate, mocked unit-test suite — not here. Mixing the two hides the
/// fact that this suite's entire value is catching real-world drift (Google changing the
/// response shape, the endpoint moving, encoding issues, etc.).
///
/// These tests are network-dependent and can be flaky in sandboxed/offline CI runners. They
/// are tagged so CI can run them on a separate, slower lane (e.g. nightly) instead of gating
/// every PR on a third-party unofficial API being reachable.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public sealed class GoogleTranslationProviderTests : IClassFixture<HttpClientFixture>
{
    // NOTE: verify this matches whatever BaseUrl your appsettings actually configures in
    // production. This is the commonly used unofficial "dt=t" endpoint; adjust if your
    // GoogleTranslateOptions points somewhere else (e.g. a self-hosted proxy).
    private static readonly GoogleTranslationOptions RealEndpointOptions = new()
    {
        BaseUrl = "https://translate.googleapis.com",
        SourceLanguage = "en",
        TargetLanguage = "vi"
    };

    private readonly HttpClient _httpClient;

    public GoogleTranslationProviderTests(HttpClientFixture fixture)
    {
        _httpClient = fixture.HttpClient;
    }

    [Fact]
    public async Task TranslateAsync_WithRealEndpoint_ReturnsNonEmptyTranslation()
    {
        var sut = CreateSut(_httpClient, RealEndpointOptions);

        var result = await sut.TranslateAsync("Hello world");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var translation = Assert.Single(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(translation.Text));
        Assert.Equal(TranslationSource.Google, translation.Source);
    }

    [Fact]
    public async Task TranslateAsync_WithRealEndpoint_TranslatesKnownPhraseIntoVietnamese()
    {
        var sut = CreateSut(_httpClient, RealEndpointOptions);

        var result = await sut.TranslateAsync("good morning");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var translation = Assert.Single(result.Value);

        // Vietnamese diacritics can be encoded as a single precomposed code point (NFC, e.g.
        // U+00E0 for "à") or as a base letter + combining accent (NFD, e.g. U+0061 U+0300).
        // Google's response and a literal typed into this source file can legitimately use
        // different forms — StringComparison.OrdinalIgnoreCase folds case but does NOT
        // normalize between NFC/NFD, so a visually-identical substring can still fail
        // Assert.Contains. Normalizing both sides at runtime fixes this regardless of which
        // form either side happens to use, and regardless of how this file's literal is
        // actually encoded on disk.
        var normalizedActual = translation.Text.Normalize(NormalizationForm.FormC);
        var normalizedExpected = "chào".Normalize(NormalizationForm.FormC);

        Assert.Contains(normalizedExpected, normalizedActual, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateAsync_WithRealEndpoint_HandlesSpecialCharactersRoundTrip()
    {
        var sut = CreateSut(_httpClient, RealEndpointOptions);

        var result = await sut.TranslateAsync("100% guaranteed & tested");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var translation = Assert.Single(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(translation.Text));
    }

    [Fact]
    public async Task TranslateAsync_WhenTextIsBlank_ReturnsFailureWithoutEverCallingTheNetwork()
    {
        // Point at a host guaranteed not to resolve (RFC 2606 reserves the ".invalid" TLD).
        // If the guard clause in TranslateAsync did NOT short-circuit before the HTTP call,
        // this would surface as an "unreachable" failure instead of the expected message —
        // so a passing test here is real proof no request left the process.
        var sut = CreateSut(_httpClient, new GoogleTranslationOptions
        {
            BaseUrl = "https://this-host-does-not-exist.invalid",
            SourceLanguage = "en",
            TargetLanguage = "vi"
        });

        var result = await sut.TranslateAsync("   ");

        Assert.True(result.IsFailure);
        Assert.Equal("Text must not be empty.", result.Error.Message);
    }

    [Fact]
    public async Task TranslateAsync_WhenHostIsUnreachable_ReturnsFailure()
    {
        var sut = CreateSut(_httpClient, new GoogleTranslationOptions
        {
            BaseUrl = "https://this-host-does-not-exist.invalid",
            SourceLanguage = "en",
            TargetLanguage = "vi"
        });

        var result = await sut.TranslateAsync("hello");

        Assert.True(result.IsFailure);
        Assert.StartsWith("Google Translate is unreachable", result.Error.Message);
    }

    [Fact]
    public async Task TranslateAsync_WhenRequestExceedsClientTimeout_ReturnsFailure()
    {
        // Scoped to this test only: a 1ms timeout would make every other test in this class
        // fail too if it were applied to the shared fixture client, so a fresh, short-lived
        // client is used here instead.
        using var shortTimeoutClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1) };
        var sut = CreateSut(shortTimeoutClient, RealEndpointOptions);

        var result = await sut.TranslateAsync("hello");

        Assert.True(result.IsFailure);
        Assert.Equal("Google Translate request timed out.", result.Error.Message);
    }

    [Fact]
    public async Task TranslateAsync_WhenCallerCancelsBeforeRequestCompletes_PropagatesCancellation()
    {
        var sut = CreateSut(_httpClient, RealEndpointOptions);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await sut.TranslateAsync("hello", cancellationSource.Token));
    }

    private static GoogleTranslationProvider CreateSut(HttpClient httpClient, GoogleTranslationOptions options) =>
        new(httpClient, Options.Create(options));
}
