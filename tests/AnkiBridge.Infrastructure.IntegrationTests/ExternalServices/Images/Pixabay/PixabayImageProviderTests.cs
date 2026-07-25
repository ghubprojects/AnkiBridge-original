using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Images.Pixabay;
using AnkiBridge.Infrastructure.IntegrationTests.Common;
using AnkiBridge.Infrastructure.IntegrationTests.Common.Fixtures;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.IntegrationTests.ExternalServices.Images.Pixabay;

/// <summary>
/// Integration tests for <see cref="PixabayImageProvider"/>. No HTTP message handler is mocked:
/// transport-failure cases use a real closed loopback port, and the successful case calls Pixabay.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public sealed class PixabayImageProviderTests(HttpClientFixture fixture) : IClassFixture<HttpClientFixture>
{
    private static readonly PixabayOptions RealEndpointOptions = new()
    {
        BaseUrl = "https://pixabay.com/api/",
        ImageType = "photo",
        Language = "en",
        SafeSearch = true
    };

    private readonly HttpClient _httpClient = fixture.HttpClient;

    [EnvironmentVariableFact("PIXABAY_API_KEY")]
    public async Task SearchAsync_WithRealEndpoint_ReturnsPixabayImages()
    {
        var apiKey = IntegrationTestEnvironment.Require("PIXABAY_API_KEY");
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var options = CopyRealOptions(apiKey);
        var sut = CreateSut(client, options);

        var result = await sut.SearchAsync("language learning", count: 3);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.InRange(result.Value.Count, 1, 3);
        Assert.All(result.Value, image =>
        {
            Assert.Equal(ImageSource.Pixabay, image.Source);
            AssertAbsoluteHttpUrl(image.PreviewUrl);
            AssertAbsoluteHttpUrl(image.FullUrl);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_WhenKeywordIsBlank_ReturnsValidationFailureBeforeCallingPixabay(
        string? keyword)
    {
        var sut = CreateSut(_httpClient, UnreachableOptions());

        var result = await sut.SearchAsync(keyword!);

        Assert.True(result.IsFailure);
        Assert.Equal("Keyword must not be empty.", result.Error.Message);
    }

    [Fact]
    public async Task SearchAsync_WhenPixabayIsUnreachable_ReturnsProviderFailure()
    {
        var sut = CreateSut(_httpClient, UnreachableOptions());

        var result = await sut.SearchAsync("hello");

        Assert.True(result.IsFailure);
        Assert.Equal("Pixabay is unavailable.", result.Error.Message);
    }

    [Fact]
    public async Task SearchAsync_WhenCallerHasCancelled_PropagatesCancellation()
    {
        var sut = CreateSut(_httpClient, CopyRealOptions(apiKey: string.Empty));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await sut.SearchAsync("hello", cancellationToken: cancellationSource.Token));
    }

    private static PixabayImageProvider CreateSut(HttpClient client, PixabayOptions options) =>
        new(client, Options.Create(options));

    private static PixabayOptions CopyRealOptions(string apiKey) => new()
    {
        BaseUrl = RealEndpointOptions.BaseUrl,
        ApiKey = apiKey,
        ImageType = RealEndpointOptions.ImageType,
        Language = RealEndpointOptions.Language,
        SafeSearch = RealEndpointOptions.SafeSearch
    };

    private static PixabayOptions UnreachableOptions() => new()
    {
        BaseUrl = "http://127.0.0.1:1/api/",
        ApiKey = "integration-test",
        ImageType = "photo",
        Language = "en",
        SafeSearch = true
    };

    private static void AssertAbsoluteHttpUrl(string value)
    {
        Assert.True(Uri.TryCreate(value, UriKind.Absolute, out var uri));
        Assert.True(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
