using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;
using AnkiBridge.Infrastructure.IntegrationTests.Common;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.IntegrationTests.ExternalServices.Dictionary.Cambridge;

/// <summary>
/// Integration tests for <see cref="CambridgeDictionaryEntryProvider"/>. The successful lookups
/// deliberately use Cambridge's live English dictionary so they cover HTTP transport, the current
/// page structure, selector compatibility, and mapping to application-level dictionary results.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public sealed class CambridgeDictionaryEntryProviderTests
{
    private static readonly CambridgeDictionaryOptions RealEndpointOptions = new()
    {
        BaseUrl = "https://dictionary.cambridge.org/dictionary"
    };

    [Fact]
    public async Task LookupAsync_WithKnownWord_ReturnsStructuredDictionaryEntry()
    {
        var sut = CreateSut(RealEndpointOptions);

        var result = await sut.LookupAsync("hello");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Contains(
            result.Value,
            candidate => candidate.Headword.Equals("hello", StringComparison.OrdinalIgnoreCase));
        var entry = result.Value.First(
            candidate => candidate.Headword.Equals("hello", StringComparison.OrdinalIgnoreCase));

        Assert.NotEqual(PartOfSpeech.Other, entry.PartOfSpeech);
        Assert.NotEmpty(entry.Definitions);
        Assert.All(entry.Definitions, definition => Assert.False(string.IsNullOrWhiteSpace(definition.Text)));
        Assert.Contains(entry.Definitions, definition => definition.Examples.Count > 0);
        Assert.NotEmpty(entry.Pronunciations);
        Assert.Contains(entry.Pronunciations, pronunciation => pronunciation.Accent == Accent.British);
        Assert.Contains(entry.Pronunciations, pronunciation => pronunciation.Accent == Accent.American);
        Assert.Contains(entry.Pronunciations, pronunciation => pronunciation.AudioUrl is not null);
        Assert.All(
            entry.Pronunciations,
            pronunciation =>
            {
                Assert.Contains(pronunciation.Accent, new[] { Accent.British, Accent.American });
                Assert.False(string.IsNullOrWhiteSpace(pronunciation.Ipa));

                if (pronunciation.AudioUrl is not null)
                {
                    Assert.True(Uri.TryCreate(pronunciation.AudioUrl, UriKind.Absolute, out var audioUri));
                    Assert.Equal(Uri.UriSchemeHttps, audioUri.Scheme);
                }
            });
    }

    [Fact]
    public async Task LookupAsync_WithMultiWordHeadword_ReturnsMatchingEntry()
    {
        var sut = CreateSut(RealEndpointOptions);

        var result = await sut.LookupAsync("  ICE CREAM  ");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Contains(
            result.Value,
            entry => entry.Headword.Equals("ice cream", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LookupAsync_WhenHeadwordIsBlank_ReturnsValidationFailureWithoutCallingCambridge(
        string? headword)
    {
        var sut = CreateSut(UnreachableEndpointOptions());

        var result = await sut.LookupAsync(headword!);

        Assert.True(result.IsFailure);
        Assert.Equal("Headword must not be empty.", result.Error.Message);
    }

    [Fact]
    public async Task LookupAsync_WhenCambridgeIsUnreachable_ReturnsProviderFailure()
    {
        var sut = CreateSut(UnreachableEndpointOptions());

        var result = await sut.LookupAsync("hello");

        Assert.True(result.IsFailure);
        Assert.Equal("Cambridge Dictionary is unavailable.", result.Error.Message);
    }

    [Fact]
    public async Task LookupAsync_WhenCallerHasCancelled_ReturnsProviderFailure()
    {
        var sut = CreateSut(RealEndpointOptions);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var result = await sut.LookupAsync("hello", cancellationSource.Token);

        Assert.True(result.IsFailure);
        Assert.Equal("Cambridge Dictionary is unavailable.", result.Error.Message);
    }

    private static CambridgeDictionaryEntryProvider CreateSut(CambridgeDictionaryOptions options) =>
        new(Options.Create(options));

    private static CambridgeDictionaryOptions UnreachableEndpointOptions() => new()
    {
        BaseUrl = "http://127.0.0.1:1/dictionary"
    };
}
