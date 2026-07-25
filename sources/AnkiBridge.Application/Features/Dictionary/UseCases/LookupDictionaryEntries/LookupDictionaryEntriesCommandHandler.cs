using AnkiBridge.Application.Abstractions.Dictionary;
using AnkiBridge.Application.Abstractions.Images;
using AnkiBridge.Application.Abstractions.Speech;
using AnkiBridge.Application.Abstractions.Translation;
using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.LookupDictionaryEntries;

public sealed class LookupDictionaryEntriesCommandHandler(
    IDictionaryEntryProvider dictionaryEntryProvider,
    ITranslationProvider translationProvider,
    IIpaProvider ipaProvider,
    IAudioProvider audioProvider,
    IImageProvider imageProvider,
    IDictionaryEntryRepository repository)
    : IRequestHandler<LookupDictionaryEntriesCommand, Result>
{
    public async Task<Result> Handle(LookupDictionaryEntriesCommand command, CancellationToken cancellationToken)
    {
        var headword = command.Headword.Trim();

        var lookupResult = await dictionaryEntryProvider.LookupAsync(headword, cancellationToken);
        if (lookupResult.IsFailure)
            return lookupResult;

        var foundEntries = lookupResult.Value;
        if (foundEntries.Count == 0)
            return Result.Failure("No dictionary entries were found.");

        foreach (var foundEntry in foundEntries)
        {
            var entryExists = await repository.ExistsAsync(
                foundEntry.Headword.Trim(),
                foundEntry.PartOfSpeech,
                foundEntry.Source,
                cancellationToken);

            if (entryExists)
            {
                return Result.Failure(
                    $"Dictionary entry '{foundEntry.Headword}' ({foundEntry.PartOfSpeech}, {foundEntry.Source}) already exists.",
                    ErrorType.Conflict);
            }
        }

        var translateResult = await translationProvider.TranslateAsync(headword, cancellationToken);
        var foundTranslations = translateResult.IsSuccess ? translateResult.Value : [];

        var imageSearchResult = await imageProvider.SearchAsync(headword, count: 3, cancellationToken: cancellationToken);
        var foundImages = imageSearchResult.IsSuccess ? imageSearchResult.Value : [];

        var isSingleWord = headword.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 1;

        foreach (var foundEntry in foundEntries)
        {
            var entryResult = DictionaryEntry.Create(foundEntry.Headword, foundEntry.PartOfSpeech, foundEntry.Source);
            if (entryResult.IsFailure)
                return entryResult;

            var entry = entryResult.Value;

            if (isSingleWord)
            {
                foreach (var pronunciation in foundEntry.Pronunciations)
                {
                    AudioSource? audioSource = null;

                    if (!string.IsNullOrWhiteSpace(pronunciation.AudioUrl))
                        audioSource = Enum.TryParse<AudioSource>(foundEntry.Source.ToString(), out var resolvedAudioSource)
                            ? resolvedAudioSource
                            : AudioSource.Unknown;

                    var addPronunciationResult = entry.AddPronunciation(
                        pronunciation.Ipa,
                        pronunciation.Accent,
                        pronunciation.AudioUrl,
                        audioSource);

                    if (addPronunciationResult.IsFailure)
                        return addPronunciationResult;
                }
            }
            else
            {
                foreach (var accent in new[] { Accent.British, Accent.American })
                {
                    var ipaResult = await ipaProvider.TranscribeAsync(headword, accent, cancellationToken);
                    var audioResult = audioProvider.Synthesize(headword);

                    if (ipaResult.IsFailure && audioResult.IsFailure)
                        continue;

                    var addPronunciationResult = entry.AddPronunciation(
                        ipaResult.Value,
                        accent,
                        audioResult.Value.Url,
                        audioResult.Value.Source);

                    if (addPronunciationResult.IsFailure)
                        return addPronunciationResult;
                }
            }

            foreach (var definition in foundEntry.Definitions)
            {
                var definitionResult = entry.AddDefinition(definition.Text, definition.Examples);
                if (definitionResult.IsFailure)
                    return definitionResult;
            }

            foreach (var translation in foundTranslations)
            {
                var addTranslationResult = entry.AddTranslation(translation.Text, translation.Source);
                if (addTranslationResult.IsFailure)
                    return addTranslationResult;
            }

            foreach (var image in foundImages)
            {
                var addImageResult = entry.AddImage(image.FullUrl, image.Source);
                if (addImageResult.IsFailure)
                    return addImageResult;
            }

            repository.Add(entry);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
