using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;
using AnkiBridge.Infrastructure.ExternalServices.Translation;
using AnkiBridge.Infrastructure.ExternalServices.Translation.Google;
using AnkiBridge.Infrastructure.IntegrationTests.Common;
using AnkiBridge.Infrastructure.IntegrationTests.Common.Fixtures;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.IntegrationTests.ExternalServices.Translation;

/// <summary>
/// Integration tests for <see cref="FallbackTranslationProvider"/> composed with its concrete
/// Cambridge and Google providers. These tests use real HTTP clients and provider implementations;
/// no dependency or HTTP message handler is mocked.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public sealed class FallbackTranslationProviderTests : IClassFixture<HttpClientFixture>
{
    private static readonly CambridgeDictionaryOptions RealCambridgeOptions = new()
    {
        BaseUrl = "https://dictionary.cambridge.org/dictionary"
    };

    private static readonly GoogleTranslationOptions RealGoogleOptions = new()
    {
        BaseUrl = "https://translate.googleapis.com",
        SourceLanguage = "en",
        TargetLanguage = "vi"
    };

    private readonly HttpClient _httpClient;

    public FallbackTranslationProviderTests(HttpClientFixture fixture)
    {
        _httpClient = fixture.HttpClient;
    }

    [Fact]
    public async Task TranslateAsync_WhenCambridgeHasTranslations_ReturnsCambridgeResults()
    {
        var sut = CreateSut(_httpClient, RealCambridgeOptions, RealGoogleOptions);

        var result = await sut.TranslateAsync("hello");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.NotEmpty(result.Value);
        Assert.All(result.Value, translation => Assert.Equal(TranslationSource.Cambridge, translation.Source));
    }

    [Fact]
    public async Task TranslateAsync_WhenCambridgeIsUnavailable_FallsBackToGoogle()
    {
        var sut = CreateSut(_httpClient, UnreachableCambridgeOptions(), RealGoogleOptions);

        var result = await sut.TranslateAsync("good morning");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var translation = Assert.Single(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(translation.Text));
        Assert.Equal(TranslationSource.Google, translation.Source);
    }

    [Fact]
    public async Task TranslateAsync_WhenCambridgeReturnsNoDictionaryTranslation_FallsBackToGoogle()
    {
        var sut = CreateSut(_httpClient, RealCambridgeOptions, RealGoogleOptions);

        var result = await sut.TranslateAsync("integration tests improve confidence");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var translation = Assert.Single(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(translation.Text));
        Assert.Equal(TranslationSource.Google, translation.Source);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TranslateAsync_WhenTextIsBlank_ReturnsValidationFailureBeforeEitherProviderIsCalled(
        string? text)
    {
        var sut = CreateSut(
            _httpClient,
            UnreachableCambridgeOptions(),
            UnreachableGoogleOptions());

        var result = await sut.TranslateAsync(text!);

        Assert.True(result.IsFailure);
        Assert.Equal("Text must not be empty.", result.Error.Message);
    }

    [Fact]
    public async Task TranslateAsync_WhenBothProvidersAreUnavailable_ReturnsGoogleFailure()
    {
        var sut = CreateSut(
            _httpClient,
            UnreachableCambridgeOptions(),
            UnreachableGoogleOptions());

        var result = await sut.TranslateAsync("hello");

        Assert.True(result.IsFailure);
        Assert.StartsWith("Google Translate is unreachable", result.Error.Message);
    }

    [Fact]
    public async Task TranslateAsync_WhenCallerHasCancelled_PropagatesCancellationFromFallbackProvider()
    {
        var sut = CreateSut(_httpClient, RealCambridgeOptions, RealGoogleOptions);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await sut.TranslateAsync("hello", cancellationSource.Token));
    }

    private static FallbackTranslationProvider CreateSut(
        HttpClient httpClient,
        CambridgeDictionaryOptions cambridgeOptions,
        GoogleTranslationOptions googleOptions)
    {
        var cambridge = new CambridgeDictionaryTranslationProvider(Options.Create(cambridgeOptions));
        var google = new GoogleTranslationProvider(httpClient, Options.Create(googleOptions));

        return new FallbackTranslationProvider(cambridge, google);
    }

    private static CambridgeDictionaryOptions UnreachableCambridgeOptions() => new()
    {
        BaseUrl = "http://127.0.0.1:1/dictionary"
    };

    private static GoogleTranslationOptions UnreachableGoogleOptions() => new()
    {
        BaseUrl = "http://127.0.0.1:1",
        SourceLanguage = "en",
        TargetLanguage = "vi"
    };
}
