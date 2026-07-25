using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;
using AnkiBridge.Infrastructure.IntegrationTests.Common;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.IntegrationTests.ExternalServices.Dictionary.Cambridge;

/// <summary>
/// Integration tests for <see cref="CambridgeDictionaryIpaProvider"/>. Successful cases
/// resolve every word from Cambridge's live English dictionary and therefore exercise URL
/// construction, concurrent HTTP lookups, live HTML parsing, and phrase composition together.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public sealed class CambridgeDictionaryPhraseIpaProviderTests
{
    private static readonly CambridgeDictionaryOptions RealEndpointOptions = new()
    {
        BaseUrl = "https://dictionary.cambridge.org/dictionary"
    };

    [Theory]
    [InlineData(Accent.British)]
    [InlineData(Accent.American)]
    public async Task ResolveAsync_WithKnownPhrase_ReturnsIpaForEveryWord(Accent accent)
    {
        var sut = CreateSut(RealEndpointOptions);

        var result = await sut.TranscribeAsync("hello world", accent);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.False(string.IsNullOrWhiteSpace(result.Value));
        Assert.Equal(2, result.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespaceAndPunctuation_NormalizesWordsBeforeLookup()
    {
        var sut = CreateSut(RealEndpointOptions);

        var result = await sut.TranscribeAsync("  hello,   world!  ", Accent.British);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Equal(2, result.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveAsync_WhenPhraseIsBlank_ReturnsValidationFailureWithoutCallingCambridge(
        string? phrase)
    {
        var sut = CreateSut(UnreachableEndpointOptions());

        var result = await sut.TranscribeAsync(phrase!, Accent.British);

        Assert.True(result.IsFailure);
        Assert.Equal("Headword phrase must not be empty.", result.Error.Message);
    }

    [Fact]
    public async Task ResolveAsync_WhenPhraseContainsOnlyPunctuation_ReturnsNoWordFailure()
    {
        var sut = CreateSut(UnreachableEndpointOptions());

        var result = await sut.TranscribeAsync("... ,!? ;: \" ()", Accent.British);

        Assert.True(result.IsFailure);
        Assert.Equal("No word found in the given headword phrase.", result.Error.Message);
    }

    [Fact]
    public async Task ResolveAsync_WhenCambridgeIsUnreachable_ReportsTheWordThatCouldNotBeResolved()
    {
        var sut = CreateSut(UnreachableEndpointOptions());

        var result = await sut.TranscribeAsync("hello", Accent.British);

        Assert.True(result.IsFailure);
        Assert.Equal("Could not find IPA for 'hello'.", result.Error.Message);
    }

    [Fact]
    public async Task ResolveAsync_WhenCallerHasCancelled_ReportsTheWordThatCouldNotBeResolved()
    {
        var sut = CreateSut(RealEndpointOptions);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var result = await sut.TranscribeAsync("hello", Accent.British, cancellationSource.Token);

        Assert.True(result.IsFailure);
        Assert.Equal("Could not find IPA for 'hello'.", result.Error.Message);
    }

    private static CambridgeDictionaryIpaProvider CreateSut(CambridgeDictionaryOptions options) =>
        new(Options.Create(options));

    private static CambridgeDictionaryOptions UnreachableEndpointOptions() => new()
    {
        BaseUrl = "http://127.0.0.1:1/dictionary"
    };
}
