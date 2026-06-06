using AnkiBridge.Application.Common.Contracts.Images;
using AnkiBridge.Application.Common.Contracts.Speech;
using AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;
using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.ScrapeDictionaryEntry;

public sealed class ScrapeDictionaryEntryCommandHandler(
    IDictionaryProvider provider,
    IDictionaryEntryRepository repository,
    ISpeechSynthesizer speech,
    IImageProvider images)
    : IRequestHandler<ScrapeDictionaryEntryCommand, Result>
{
    public async Task<Result> Handle(ScrapeDictionaryEntryCommand request, CancellationToken cancellationToken)
    {
        var word = request.Headword.Trim();

        // ── 1. Scrape ────────────────────────────────────────────────────────
        var scrapeResult = await provider.ScrapeAsync(word, cancellationToken);
        if (scrapeResult.IsFailure)
            return scrapeResult;

        var scrapedEntries = scrapeResult.Value;
        if (scrapedEntries.Count == 0)
            return Result.Failure("No entries found for the specified word.");

        // ── 2. Create entries ────────────────────────────────────────────────
        foreach (var scrapedEntry in scrapedEntries)
        {
            var createResult = DictionaryEntry.Create(
                headword: scrapedEntry.Headword,
                partOfSpeech: ParsePartOfSpeech(scrapedEntry.PartOfSpeech),
                source: DictionarySource.Cambridge);

            if (createResult.IsFailure)
                return createResult;

            var entry = createResult.Value;

            // ── 3. Pronunciations ────────────────────────────────────────────
            var pronResult = AddPronunciations(entry, scrapedEntry, word);
            if (pronResult.IsFailure)
                return pronResult;

            // ── 4. Definitions ───────────────────────────────────────────────
            var defResult = AddDefinitions(entry, scrapedEntry);
            if (defResult.IsFailure)
                return defResult;

            // ── 5. Images ────────────────────────────────────────────────────
            var imageResult = await AddImagesAsync(entry, word, cancellationToken);
            if (imageResult.IsFailure)
                return imageResult;

            await repository.AddAsync(entry, cancellationToken);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // ── Pronunciations ───────────────────────────────────────────────────────

    private Result AddPronunciations(
        DictionaryEntry entry,
        ScrapedEntry scrapedEntry,
        string word)
    {
        // Cambridge provides no audio for some phrases — guarantee at least one US entry.
        if (scrapedEntry.Pronunciations.Count == 0)
        {
            return entry.AddPronunciation(
                ipa: string.Empty,
                accent: Accent.American,
                audioUrl: speech.BuildAudioUrl(word),
                audioSource: AudioSource.Google);
        }

        foreach (var p in scrapedEntry.Pronunciations)
        {
            var (audioUrl, audioSource) = p.AudioUrl is not null
                ? (p.AudioUrl, AudioSource.Cambridge)
                : (speech.BuildAudioUrl(word), AudioSource.Google);

            var result = entry.AddPronunciation(
                ipa: p.Ipa,
                accent: p.Accent,
                audioUrl: audioUrl,
                audioSource: audioSource);

            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    // ── Definitions ──────────────────────────────────────────────────────────

    private static Result AddDefinitions(DictionaryEntry entry, ScrapedEntry scrapedEntry)
    {
        foreach (var d in scrapedEntry.Definitions)
        {
            var result = entry.AddDefinition(d.Text, d.Examples);
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    // ── Images ───────────────────────────────────────────────────────────────

    private async Task<Result> AddImagesAsync(
        DictionaryEntry entry,
        string word,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ImageResult> imageResults;

        try
        {
            imageResults = await images.SearchAsync(word, count: 3, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Image search failure is non-fatal — log and continue without images.
            // The user can trigger a reload from the UI.
            return Result.Success();
        }

        foreach (var image in imageResults)
        {
            var imageSource = image.Provider switch
            {
                "Pixabay" => ImageSource.Pixabay,
                "Pexels" => ImageSource.Pexels,
                _ => ImageSource.Pixabay
            };

            var result = entry.AddImage(image.FullUrl, imageSource);
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    // ── PartOfSpeech mapping ─────────────────────────────────────────────────

    private static PartOfSpeech ParsePartOfSpeech(string raw) =>
        raw.ToLowerInvariant().Trim() switch
        {
            "noun" => PartOfSpeech.Noun,
            "verb" => PartOfSpeech.Verb,
            "adjective" => PartOfSpeech.Adjective,
            "adverb" => PartOfSpeech.Adverb,
            "pronoun" => PartOfSpeech.Pronoun,
            "determiner" => PartOfSpeech.Determiner,
            "preposition" => PartOfSpeech.Preposition,
            "conjunction" => PartOfSpeech.Conjunction,
            "auxiliary verb" => PartOfSpeech.AuxiliaryVerb,
            "modal verb" => PartOfSpeech.ModalVerb,
            "phrasal verb" => PartOfSpeech.PhrasalVerb,
            "number" => PartOfSpeech.Number,
            "ordinal number" => PartOfSpeech.OrdinalNumber,
            "collocation" => PartOfSpeech.Collocation,
            "idiom" => PartOfSpeech.Idiom,
            "phrase" => PartOfSpeech.Phrase,
            "prefix" => PartOfSpeech.Prefix,
            "suffix" => PartOfSpeech.Suffix,
            "exclamation" => PartOfSpeech.Exclamation,
            _ => PartOfSpeech.Other
        };
}