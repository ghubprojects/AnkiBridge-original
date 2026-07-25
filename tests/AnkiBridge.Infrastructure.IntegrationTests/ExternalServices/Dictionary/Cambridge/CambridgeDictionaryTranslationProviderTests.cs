using System.Text;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;
using AnkiBridge.Infrastructure.IntegrationTests.Common;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.IntegrationTests.ExternalServices.Dictionary.Cambridge;

/// <summary>
/// Genuine integration tests for <see cref="CambridgeDictionaryTranslationProvider"/>: every
/// successful lookup exercises the real Cambridge Dictionary website and parses its current
/// HTML. No local HTTP server, canned response body, or stubbed HTTP handler is used.
///
/// These tests intentionally cover the boundary where the provider adds value: URL construction,
/// real HTTP transport, Cambridge's live markup, translation extraction, and result mapping. Tests
/// that exercise parsing against fixed HTML samples belong in a separate unit-test suite.
///
/// The suite is network-dependent and can fail when Cambridge is unavailable, blocks the runner,
/// or changes its markup. It is tagged so CI can run it separately from the deterministic test
/// suite instead of making every pull request depend on a third-party website.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public sealed class CambridgeDictionaryTranslationProviderTests
{
    private static readonly CambridgeDictionaryOptions RealEndpointOptions = new()
    {
        BaseUrl = "https://dictionary.cambridge.org/dictionary"
    };

    [Fact]
    public async Task TranslateAsync_WithRealEndpoint_ReturnsNonEmptyTranslations()
    {
        var sut = CreateSut(RealEndpointOptions);

        var result = await sut.TranslateAsync("hello");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.NotEmpty(result.Value);
        Assert.All(
            result.Value,
            translation =>
            {
                Assert.False(string.IsNullOrWhiteSpace(translation.Text));
                Assert.Equal(TranslationSource.Cambridge, translation.Source);
            });
    }

    [Fact]
    public async Task TranslateAsync_WithRealEndpoint_TranslatesKnownWordIntoVietnamese()
    {
        var sut = CreateSut(RealEndpointOptions);

        var result = await sut.TranslateAsync("hello");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var normalizedExpected = "xin ch\u00E0o".Normalize(NormalizationForm.FormC);

        Assert.Contains(
            result.Value,
            translation => translation.Text
                .Normalize(NormalizationForm.FormC)
                .Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TranslateAsync_WithRealEndpoint_HandlesSurroundingWhitespaceAndCasing()
    {
        var sut = CreateSut(RealEndpointOptions);

        var result = await sut.TranslateAsync("   GOOD MORNING   ");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.NotEmpty(result.Value);
        Assert.All(result.Value, translation => Assert.Equal(TranslationSource.Cambridge, translation.Source));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TranslateAsync_WhenTextIsBlank_ReturnsFailureWithoutCallingCambridge(string? text)
    {
        // A loopback port that should refuse the connection makes the guard observable: if the
        // provider attempts a request, the result will be the generic unavailable failure instead.
        var sut = CreateSut(new CambridgeDictionaryOptions
        {
            BaseUrl = "http://127.0.0.1:1/dictionary"
        });

        var result = await sut.TranslateAsync(text!);

        Assert.True(result.IsFailure);
        Assert.Equal("Text must not be empty.", result.Error.Message);
    }

    [Fact]
    public async Task TranslateAsync_WhenEndpointIsUnreachable_ReturnsFailure()
    {
        var sut = CreateSut(new CambridgeDictionaryOptions
        {
            BaseUrl = "http://127.0.0.1:1/dictionary"
        });

        var result = await sut.TranslateAsync("hello");

        Assert.True(result.IsFailure);
        Assert.Equal("Cambridge Dictionary is unavailable.", result.Error.Message);
    }

    [Fact]
    public async Task TranslateAsync_WhenCallerCancels_ReturnsUnavailableFailure()
    {
        // CambridgeDictionaryProviderBase currently maps every exception, including caller
        // cancellation, to its provider-level unavailable result. This pins that behavior so a
        // future decision to propagate cancellation is made explicitly rather than accidentally.
        var sut = CreateSut(RealEndpointOptions);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var result = await sut.TranslateAsync("hello", cancellationSource.Token);

        Assert.True(result.IsFailure);
        Assert.Equal("Cambridge Dictionary is unavailable.", result.Error.Message);
    }

    private static CambridgeDictionaryTranslationProvider CreateSut(CambridgeDictionaryOptions options) =>
        new(Options.Create(options));
}
