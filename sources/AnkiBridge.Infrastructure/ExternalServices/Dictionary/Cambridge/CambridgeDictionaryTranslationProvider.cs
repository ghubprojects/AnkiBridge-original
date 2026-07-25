using AngleSharp.Dom;
using AnkiBridge.Application.Abstractions.Translation;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.Extensions;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;

public sealed class CambridgeDictionaryTranslationProvider(IOptions<CambridgeDictionaryOptions> options)
    : CambridgeDictionaryProviderBase(options), ITranslationProvider
{
    private const string Section = "english-vietnamese";

    public async Task<Result<IReadOnlyList<TranslationResult>>> TranslateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<IReadOnlyList<TranslationResult>>("Text must not be empty.");

        var url = BuildUrl(Section, text);
        return await FetchAndParseAsync(url, ParseDocument, cancellationToken);
    }

    private Result<IReadOnlyList<TranslationResult>> ParseDocument(IDocument document) =>
        document.QueryAll(CambridgeDictionarySelectors.EnglishVietnameseEntryAndIdiomBlocks)
            .SelectMany(ParseTranslations)
            .ToList();

    private List<TranslationResult> ParseTranslations(IElement block) =>
        block.QueryAll(CambridgeDictionarySelectors.DefinitionBlock)
            .Select(ExtractTranslation)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => new TranslationResult(text!, TranslationSource.Cambridge))
            .ToList();

    private static string ExtractTranslation(IElement definitionBlock) =>
        CleanText(definitionBlock.QueryFirstText(CambridgeDictionarySelectors.Translation));
}
