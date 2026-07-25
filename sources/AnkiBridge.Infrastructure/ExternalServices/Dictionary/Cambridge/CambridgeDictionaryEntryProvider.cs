using AngleSharp.Dom;
using AnkiBridge.Application.Abstractions.Dictionary;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.Extensions;
using AnkiBridge.Shared.Results;
using Humanizer;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;

public sealed class CambridgeDictionaryEntryProvider(IOptions<CambridgeDictionaryOptions> options)
    : CambridgeDictionaryProviderBase(options), IDictionaryEntryProvider
{
    private const string Section = "english";

    private static readonly (string[] ContainerSelectors, Accent Accent)[] PronunciationContainers =
    [
        (CambridgeDictionarySelectors.BritishPronunciationBlock, Accent.British),
        (CambridgeDictionarySelectors.AmericanPronunciationBlock, Accent.American),
    ];

    public async Task<Result<IReadOnlyList<DictionaryEntryResult>>> LookupAsync(
       string headword,
       CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(headword))
            return Result.Failure<IReadOnlyList<DictionaryEntryResult>>("Headword must not be empty.");

        var url = BuildUrl(Section, headword);

        return await FetchAndParseAsync(url, ParseDocument, cancellationToken);
    }

    private Result<IReadOnlyList<DictionaryEntryResult>> ParseDocument(IDocument document)
        => document.QueryAll(CambridgeDictionarySelectors.EnglishEntryAndIdiomBlocks)
            .Select(ParseEntryBlock)
            .OfType<DictionaryEntryResult>()
            .ToList();

    private DictionaryEntryResult? ParseEntryBlock(IElement block)
    {
        var headword = block.QueryFirstText(CambridgeDictionarySelectors.Headword);
        if (string.IsNullOrWhiteSpace(headword))
            return null;

        return new DictionaryEntryResult(
            Headword: headword,
            PartOfSpeech: ParsePartOfSpeech(block),
            Source: DictionarySource.Cambridge,
            Pronunciations: ParsePronunciations(block),
            Definitions: ParseDefinitions(block));
    }

    private static PartOfSpeech ParsePartOfSpeech(IElement block)
    {
        var rawLabel = block.QueryFirstText(CambridgeDictionarySelectors.PartOfSpeech);
        if (string.IsNullOrWhiteSpace(rawLabel))
            return PartOfSpeech.Other;

        try
        {
            return rawLabel.DehumanizeTo<PartOfSpeech>();
        }
        catch (ArgumentException)
        {
            return PartOfSpeech.Other;
        }
    }

    private List<DictionaryPronunciationResult> ParsePronunciations(IElement block) =>
        PronunciationContainers
            .Select(container => TryGetPronunciation(block, container.ContainerSelectors, container.Accent))
            .OfType<DictionaryPronunciationResult>()
            .ToList();

    private DictionaryPronunciationResult? TryGetPronunciation(
          IElement block,
          string[] containerSelectors,
          Accent accent)
    {
        var container = block.QueryFirst(containerSelectors);
        if (container is null)
            return null;

        var ipa = CleanText(container.QueryFirstText(CambridgeDictionarySelectors.Ipa));
        if (string.IsNullOrWhiteSpace(ipa))
            return null;

        var audioSrc = container
            .QuerySelectorAll("source")
            .FirstOrDefault(s => s.GetAttribute("type") == "audio/mpeg")
            ?.GetAttribute("src");

        return new DictionaryPronunciationResult(
            accent,
            ipa,
            audioSrc is not null ? ToAbsoluteUrl(audioSrc) : null);
    }

    private List<DictionaryDefinitionResult> ParseDefinitions(IElement block) =>
        block.QueryAll(CambridgeDictionarySelectors.DefinitionBlock)
            .Select(ParseDefinitionBlock)
            .OfType<DictionaryDefinitionResult>()
            .ToList();

    private DictionaryDefinitionResult? ParseDefinitionBlock(IElement defBlock)
    {
        var text = defBlock.QueryFirstText(CambridgeDictionarySelectors.Definition);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var examples = defBlock.QueryAll(CambridgeDictionarySelectors.Example)
            .Select(e => CleanText(e.TextContent))
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList()
            .AsReadOnly();

        return new DictionaryDefinitionResult(CleanText(text), examples);
    }

}
