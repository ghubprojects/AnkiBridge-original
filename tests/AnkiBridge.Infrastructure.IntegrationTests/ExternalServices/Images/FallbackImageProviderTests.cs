using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Images;
using AnkiBridge.Infrastructure.ExternalServices.Images.Pexels;
using AnkiBridge.Infrastructure.ExternalServices.Images.Pixabay;
using AnkiBridge.Infrastructure.IntegrationTests.Common;
using AnkiBridge.Infrastructure.IntegrationTests.Common.Fixtures;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.IntegrationTests.ExternalServices.Images;

/// <summary>
/// Integration tests for <see cref="FallbackImageProvider"/> composed from its concrete providers.
/// The tests use real HTTP clients and do not substitute either provider or its message handler.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public sealed class FallbackImageProviderTests : IClassFixture<HttpClientFixture>
{
    private readonly HttpClient _httpClient;

    public FallbackImageProviderTests(HttpClientFixture fixture)
    {
        _httpClient = fixture.HttpClient;
    }

    [EnvironmentVariableFact("PIXABAY_API_KEY")]
    public async Task SearchAsync_WhenPixabaySucceeds_ReturnsPrimaryProviderResults()
    {
        var pixabayApiKey = IntegrationTestEnvironment.Require("PIXABAY_API_KEY");
        using var pixabayClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var pexelsClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var sut = CreateSut(
            pixabayClient,
            RealPixabayOptions(pixabayApiKey),
            pexelsClient,
            UnreachablePexelsOptions());

        var result = await sut.SearchAsync("language learning", count: 3);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.NotEmpty(result.Value);
        Assert.All(result.Value, image => Assert.Equal(ImageSource.Pixabay, image.Source));
    }

    [EnvironmentVariableFact("PEXELS_API_KEY")]
    public async Task SearchAsync_WhenPixabayIsUnavailable_FallsBackToPexels()
    {
        var pexelsApiKey = IntegrationTestEnvironment.Require("PEXELS_API_KEY");
        using var pixabayClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var pexelsClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        pexelsClient.DefaultRequestHeaders.Add("Authorization", pexelsApiKey);
        var sut = CreateSut(
            pixabayClient,
            UnreachablePixabayOptions(),
            pexelsClient,
            RealPexelsOptions());

        var result = await sut.SearchAsync("language learning", count: 3);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.NotEmpty(result.Value);
        Assert.All(result.Value, image => Assert.Equal(ImageSource.Pexels, image.Source));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_WhenKeywordIsBlank_ReturnsValidationFailureBeforeCallingEitherProvider(
        string? keyword)
    {
        var sut = CreateSut(
            _httpClient,
            UnreachablePixabayOptions(),
            _httpClient,
            UnreachablePexelsOptions());

        var result = await sut.SearchAsync(keyword!);

        Assert.True(result.IsFailure);
        Assert.Equal("Keyword must not be empty.", result.Error.Message);
    }

    [Fact]
    public async Task SearchAsync_WhenBothProvidersAreUnavailable_ReturnsSecondaryProviderFailure()
    {
        var sut = CreateSut(
            _httpClient,
            UnreachablePixabayOptions(),
            _httpClient,
            UnreachablePexelsOptions());

        var result = await sut.SearchAsync("hello");

        Assert.True(result.IsFailure);
        Assert.Equal("Pexels is unavailable.", result.Error.Message);
    }

    [Fact]
    public async Task SearchAsync_WhenCallerHasCancelled_PropagatesCancellationFromPrimaryProvider()
    {
        var sut = CreateSut(
            _httpClient,
            RealPixabayOptions(apiKey: string.Empty),
            _httpClient,
            RealPexelsOptions());
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await sut.SearchAsync("hello", cancellationToken: cancellationSource.Token));
    }

    private static FallbackImageProvider CreateSut(
        HttpClient pixabayClient,
        PixabayOptions pixabayOptions,
        HttpClient pexelsClient,
        PexelsOptions pexelsOptions)
    {
        var pixabay = new PixabayImageProvider(pixabayClient, Options.Create(pixabayOptions));
        var pexels = new PexelsImageProvider(pexelsClient, Options.Create(pexelsOptions));

        return new FallbackImageProvider(pixabay, pexels);
    }

    private static PixabayOptions RealPixabayOptions(string apiKey) => new()
    {
        BaseUrl = "https://pixabay.com/api/",
        ApiKey = apiKey,
        ImageType = "photo",
        Language = "en",
        SafeSearch = true
    };

    private static PexelsOptions RealPexelsOptions() => new()
    {
        BaseUrl = "https://api.pexels.com/v1/search",
        Locale = "en-US",
        Orientation = string.Empty
    };

    private static PixabayOptions UnreachablePixabayOptions() => new()
    {
        BaseUrl = "http://127.0.0.1:1/api/",
        ApiKey = "integration-test",
        ImageType = "photo",
        Language = "en",
        SafeSearch = true
    };

    private static PexelsOptions UnreachablePexelsOptions() => new()
    {
        BaseUrl = "http://127.0.0.1:1/search",
        Locale = "en-US"
    };
}
