using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.ExternalServices.Speech.Google;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.UnitTests.ExternalServices.Speech.Google;

/// <summary>
/// Unit tests for <see cref="GoogleSpeechProvider"/>.
///
/// Unlike <c>GoogleTranslationProvider</c>, <see cref="GoogleSpeechProvider.Synthesize"/>
/// makes NO network call — it is a pure, synchronous function that builds a URL string from its
/// inputs and configured options. There is nothing to "integrate" with here, so this class
/// belongs in the unit-test suite rather than the integration-test suite: every test below is
/// fully deterministic and needs no network access.
///
/// (Assumes <c>SpeechSynthesisResult</c> exposes <c>Url</c>/<c>Source</c> properties matching the
/// positional constructor call <c>new SpeechSynthesisResult(url, AudioSource.Google)</c> in
/// production — rename if your actual record uses different property names, e.g. AudioUrl.)
///
/// If you also want to verify the URLs this class produces actually resolve to playable audio on
/// Google's real endpoint, that's a separate, genuine integration test — happy to add one that
/// does a real HttpClient.GetAsync against a generated URL and checks the response is audio,
/// following the same pattern as GoogleTranslationProviderTests.
/// </summary>
public sealed class GoogleSpeechSynthesisProviderTests
{
    // Mirrors the shape of the real (unofficial) Google Translate TTS endpoint this class
    // targets: https://translate.google.com/translate_tts?ie=UTF-8&q=...&tl=...&client=tw-ob
    private static readonly GoogleSpeechOptions DefaultOptions = new()
    {
        BaseUrl = "https://translate.google.com/translate_tts",
        DefaultLanguage = "en",
        Client = "tw-ob"
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Synthesize_WhenTextIsBlank_ReturnsFailure(string? text)
    {
        var sut = CreateSut();

        var result = sut.Synthesize(text!);

        Assert.True(result.IsFailure);
        Assert.Equal("Text must not be empty.", result.Error.Message);
    }

    [Fact]
    public void Synthesize_WithValidText_ReturnsExpectedUrlAndSource()
    {
        var sut = CreateSut();

        var result = sut.Synthesize("hello world");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "https://translate.google.com/translate_tts?ie=UTF-8&q=hello%20world&tl=en&client=tw-ob",
            result.Value.Url);
        Assert.Equal(AudioSource.Google, result.Value.Source);
    }

    [Fact]
    public void Synthesize_TrimsSurroundingWhitespaceBeforeEncoding()
    {
        var sut = CreateSut();

        var result = sut.Synthesize("   hello world   ");

        Assert.True(result.IsSuccess);
        Assert.Contains("q=hello%20world&", result.Value.Url);
        // No leading/trailing %20 leaking in from the untrimmed input.
        Assert.DoesNotContain("q=%20", result.Value.Url);
    }

    [Fact]
    public void Synthesize_EncodesTextUsingTheSameEscapingTheProductionCodeUses()
    {
        var sut = CreateSut();
        const string text = "xin chào & tạm biệt?";

        var result = sut.Synthesize(text);

        Assert.True(result.IsSuccess);
        // Computed from the very same string instance passed to the SUT, so this can never fall
        // into the NFC/NFD normalization trap a hand-typed expected literal would risk — both
        // sides encode identical in-memory bytes.
        var expectedEncodedText = Uri.EscapeDataString(text.Trim());
        Assert.Contains($"q={expectedEncodedText}&", result.Value.Url);
    }

    [Fact]
    public void Synthesize_WhenLanguageCodeIsOmitted_UsesTheParameterDefaultInsteadOfOptionsDefault()
    {
        // "en" lives on the method signature's default parameter value, so unless a caller
        // explicitly passes a blank languageCode, _options.DefaultLanguage is never consulted —
        // even when it's configured to something else. This pins that easy-to-misread behavior.
        var sut = CreateSut(new GoogleSpeechOptions
        {
            BaseUrl = DefaultOptions.BaseUrl,
            DefaultLanguage = "vi",
            Client = DefaultOptions.Client
        });

        var result = sut.Synthesize("hello");

        Assert.True(result.IsSuccess);
        Assert.Contains("tl=en", result.Value.Url);
        Assert.DoesNotContain("tl=vi", result.Value.Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Synthesize_WhenLanguageCodeIsExplicitlyBlank_FallsBackToOptionsDefaultLanguage(string? languageCode)
    {
        var sut = CreateSut(new GoogleSpeechOptions
        {
            BaseUrl = DefaultOptions.BaseUrl,
            DefaultLanguage = "vi",
            Client = DefaultOptions.Client
        });

        var result = sut.Synthesize("hello", languageCode!);

        Assert.True(result.IsSuccess);
        Assert.Contains("tl=vi", result.Value.Url);
    }

    [Fact]
    public void Synthesize_WhenLanguageCodeIsProvided_UsesItVerbatim()
    {
        var sut = CreateSut();

        var result = sut.Synthesize("hello", "fr");

        Assert.True(result.IsSuccess);
        Assert.Contains("tl=fr", result.Value.Url);
    }

    [Fact]
    public void Synthesize_UsesBaseUrlAndClientFromOptions()
    {
        var sut = CreateSut(new GoogleSpeechOptions
        {
            BaseUrl = "https://custom-tts.example.com/synthesize",
            DefaultLanguage = "en",
            Client = "custom-client"
        });

        var result = sut.Synthesize("hello");

        Assert.True(result.IsSuccess);
        Assert.StartsWith("https://custom-tts.example.com/synthesize?", result.Value.Url);
        Assert.EndsWith("&client=custom-client", result.Value.Url);
    }

    [Fact]
    public void Synthesize_DoesNotUrlEncodeLanguageCode_PinsCurrentBehavior()
    {
        // Documents an existing gap rather than desired behavior: unlike `text`, `languageCode`
        // is interpolated into the URL as-is with no Uri.EscapeDataString call. Harmless while
        // language codes are internally controlled constants ("en", "vi", "fr"...), but worth
        // knowing if that source ever becomes less trusted.
        var sut = CreateSut();

        var result = sut.Synthesize("hello", "en&extra=1");

        Assert.True(result.IsSuccess);
        Assert.Contains("tl=en&extra=1", result.Value.Url);
    }

    private static GoogleSpeechProvider CreateSut(GoogleSpeechOptions? options = null) =>
        new(Options.Create(options ?? DefaultOptions));
}