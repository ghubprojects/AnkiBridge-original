using AngleSharp.Dom;
using AnkiBridge.Application.Abstractions.Dictionary;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.Extensions;
using AnkiBridge.Shared.Results;

using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;

public sealed class CambridgeDictionaryIpaProvider(IOptions<CambridgeDictionaryOptions> options)
    : CambridgeDictionaryProviderBase(options), IIpaProvider
{
    private const string Section = "english";

    public async Task<Result<string>> TranscribeAsync(
        string headword,
        Accent accent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(headword))
            return Result.Failure<string>("Headword phrase must not be empty.");

        var words = SplitIntoWords(headword);
        if (words.Count == 0)
            return Result.Failure<string>("No word found in the given headword phrase.");

        var ipas = await Task.WhenAll(
            words.Select(word => LookupWordIpaAsync(word, accent, cancellationToken)));

        for (var i = 0; i < words.Count; i++)
        {
            if (ipas[i] is null)
                return Result.Failure<string>($"Could not find IPA for '{words[i]}'.");
        }

        return string.Join(" ", ipas);
    }

    private async Task<string?> LookupWordIpaAsync(
        string word,
        Accent accent,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(Section, word);

        var result = await FetchAndParseAsync(url, document => ParseIpa(document, accent), cancellationToken);

        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value)
            ? result.Value
            : null;
    }

    private Result<string?> ParseIpa(IDocument document, Accent accent)
    {
        var containerSelectors = accent switch
        {
            Accent.British => CambridgeDictionarySelectors.BritishPronunciationBlock,
            Accent.American => CambridgeDictionarySelectors.AmericanPronunciationBlock,
            _ => throw new ArgumentOutOfRangeException(nameof(accent), accent, "Unsupported accent."),
        };

        foreach (var block in document.QueryAll(CambridgeDictionarySelectors.EnglishEntryBlocks))
        {
            var container = block.QueryFirst(containerSelectors);
            var ipa = container is null ? null : ExtractPreferWeakForm(container);

            if (!string.IsNullOrWhiteSpace(ipa))
                return Result.Success<string?>(ipa);
        }

        return Result.Success<string?>(null);
    }

    // Function words (e.g. "a", "and", "for") sometimes have more than one pronunciation listed
    // inside the same UK/US block: a default/weak one and one explicitly marked "strong".
    // Cambridge doesn't expose a documented, stable CSS class for that label, so detection here
    // is text-based: it reads whatever text sits right before each IPA entry and looks for
    // "weak"/"strong". Inspect the live page's markup and adjust this if it stops matching.
    private static string? ExtractPreferWeakForm(IElement container)
    {
        var ipaElements = container.QueryAll(CambridgeDictionarySelectors.Ipa).ToList();
        if (ipaElements.Count == 0)
            return null;

        if (ipaElements.Count == 1)
            return CleanIpa(ipaElements[0]);

        var weak = ipaElements.FirstOrDefault(e =>
            PrecedingLabel(container, e).Contains("weak", StringComparison.OrdinalIgnoreCase));
        if (weak is not null)
            return CleanIpa(weak);

        var notStrong = ipaElements.FirstOrDefault(e =>
            !PrecedingLabel(container, e).Contains("strong", StringComparison.OrdinalIgnoreCase));

        return CleanIpa(notStrong ?? ipaElements[0]);
    }

    private static string PrecedingLabel(IElement container, IElement ipaElement)
    {
        var fullText = container.TextContent;
        var ipaText = ipaElement.TextContent;
        var index = fullText.IndexOf(ipaText, StringComparison.Ordinal);

        return index > 0 ? fullText[..index] : string.Empty;
    }

    private static string CleanIpa(IElement element) =>
       element.TextContent.Trim();

    private static List<string> SplitIntoWords(string phrase) =>
        phrase
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('.', ',', '!', '?', ';', ':', '"', '(', ')'))
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();
}
