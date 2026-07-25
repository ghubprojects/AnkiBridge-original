using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Images.Pexels;
using AnkiBridge.Infrastructure.IntegrationTests.Common;
using AnkiBridge.Infrastructure.IntegrationTests.Common.Fixtures;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.IntegrationTests.ExternalServices.Images.Pexels;

/// <summary>
/// Integration tests for <see cref="PexelsImageProvider"/>. No HTTP message handler is mocked:
/// transport-failure cases use a real closed loopback port, and the successful case calls Pexels.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public sealed class PexelsImageProviderTests(HttpClientFixture fixture) : IClassFixture<HttpClientFixture>
{
    private static readonly PexelsOptions RealEndpointOptions = new()
    {
        BaseUrl = "https://api.pexels.com/v1/search",
        Locale = "en-US",
        Orientation = string.Empty
    };

    private readonly HttpClient _httpClient = fixture.HttpClient;

    [EnvironmentVariableFact("PEXELS_API_KEY")]
    public async Task SearchAsync_WithRealEndpoint_ReturnsPexelsImages()
    {
        var apiKey = IntegrationTestEnvironment.Require("PEXELS_API_KEY");
        using var authenticatedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        authenticatedClient.DefaultRequestHeaders.Add("Authorization", apiKey);
        var sut = CreateSut(authenticatedClient, RealEndpointOptions);

        var result = await sut.SearchAsync("language learning", count: 3);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.InRange(result.Value.Count, 1, 3);
        Assert.All(result.Value, image =>
        {
            Assert.Equal(ImageSource.Pexels, image.Source);
            AssertAbsoluteHttpUrl(image.PreviewUrl);
            AssertAbsoluteHttpUrl(image.FullUrl);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_WhenKeywordIsBlank_ReturnsValidationFailureBeforeCallingPexels(
        string? keyword)
    {
        var sut = CreateSut(_httpClient, UnreachableOptions());

        var result = await sut.SearchAsync(keyword!);

        Assert.True(result.IsFailure);
        Assert.Equal("Keyword must not be empty.", result.Error.Message);
    }

    [Fact]
    public async Task SearchAsync_WhenPexelsIsUnreachable_ReturnsProviderFailure()
    {
        var sut = CreateSut(_httpClient, UnreachableOptions());

        var result = await sut.SearchAsync("hello");

        Assert.True(result.IsFailure);
        Assert.Equal("Pexels is unavailable.", result.Error.Message);
    }

    [Fact]
    public async Task SearchAsync_WhenCallerHasCancelled_PropagatesCancellation()
    {
        var sut = CreateSut(_httpClient, RealEndpointOptions);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await sut.SearchAsync("hello", cancellationToken: cancellationSource.Token));
    }

    private static PexelsImageProvider CreateSut(HttpClient client, PexelsOptions options) =>
        new(client, Options.Create(options));

    private static PexelsOptions UnreachableOptions() => new()
    {
        BaseUrl = "http://127.0.0.1:1/search",
        Locale = "en-US"
    };

    private static void AssertAbsoluteHttpUrl(string value)
    {
        Assert.True(Uri.TryCreate(value, UriKind.Absolute, out var uri));
        Assert.True(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
