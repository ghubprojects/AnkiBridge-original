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
    IImageProvider images,
    IPhraseIpaResolver phraseIpaResolver,
    ITranslationProvider translations)
    : IRequestHandler<ScrapeDictionaryEntryCommand, Result>
{
    public async Task<Result> Handle(ScrapeDictionaryEntryCommand request, CancellationToken cancellationToken)
    {
        var word = request.Headword.Trim();

        var scrapeResult = await provider.ScrapeAsync(word, cancellationToken);
        if (scrapeResult.IsFailure)
            return scrapeResult;

        var scrapedEntries = scrapeResult.Value;
        if (scrapedEntries.Count == 0)
            return Result.Failure("No entries found for the specified word.");

        // Resolve translations once — shared across all POS entries of the same word
        var translationResults = await ResolveTranslationsAsync(word, cancellationToken);

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
            var pronResult = await AddPronunciationsAsync(entry, scrapedEntry, word, cancellationToken); // ← await
            if (pronResult.IsFailure)
                return pronResult;

            // ── 4. Definitions ───────────────────────────────────────────────
            var defResult = AddDefinitions(entry, scrapedEntry);
            if (defResult.IsFailure)
                return defResult;

            var transResult = AddTranslations(entry, translationResults);
            if (transResult.IsFailure)
                return transResult;

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

    private async Task<Result> AddPronunciationsAsync(
        DictionaryEntry entry,
        ScrapedEntry scrapedEntry,
        string word,
        CancellationToken cancellationToken)
    {
        // Tầng 1: Cambridge trả về đầy đủ — dùng luôn.
        if (scrapedEntry.Pronunciations.Count > 0)
        {
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

        // Tầng 2: Phrase không có IPA — thử ghép từng từ.
        if (IsPhrase(word))
        {
            var resolvedAny = false;

            foreach (var accent in new[] { Accent.British, Accent.American })
            {
                var ipa = await phraseIpaResolver.ResolveAsync(word, accent, cancellationToken);
                if (ipa is null) continue;

                var result = entry.AddPronunciation(
                    ipa: ipa,
                    accent: accent,
                    audioUrl: speech.BuildAudioUrl(word),
                    audioSource: AudioSource.Google);

                if (result.IsFailure)
                    return result;

                resolvedAny = true;
            }

            if (resolvedAny)
                return Result.Success();
        }

        // Tầng 3: Không resolve được — TTS + IPA trống, user có thể bổ sung sau.
        return entry.AddPronunciation(
            ipa: string.Empty,
            accent: Accent.American,
            audioUrl: speech.BuildAudioUrl(word),
            audioSource: AudioSource.Google);
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

    // ── Translations ─────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<TranslationLookupResult>> ResolveTranslationsAsync(
        string word, CancellationToken ct)
    {
        var result = await translations.GetTranslationsAsync(word, ct);

        // Non-fatal — user can add manually from UI
        return result.IsSuccess ? result.Value : [];
    }

    private static Result AddTranslations(
        DictionaryEntry entry,
        IReadOnlyList<TranslationLookupResult> translationResults)
    {
        foreach (var t in translationResults)
        {
            var result = entry.AddTranslation(t.Text, t.Source);
            if (result.IsFailure) return result;
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
        catch (Exception)
        {
            return Result.Success();
        }

        foreach (var image in imageResults)
        {
            var result = entry.AddImage(image.FullUrl, image.Source);
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsPhrase(string word) => word.Contains(' ');

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