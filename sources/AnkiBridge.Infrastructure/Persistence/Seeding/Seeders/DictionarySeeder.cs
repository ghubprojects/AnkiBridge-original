using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Infrastructure.Persistence.Abstractions;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using AnkiBridge.Infrastructure.Persistence.Seeding.Helpers;
using AnkiBridge.Infrastructure.Persistence.Seeding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnkiBridge.Infrastructure.Persistence.Seeding.Seeders;

public sealed class DictionarySeeder(ILogger<DictionarySeeder> logger) : IDbSeeder
{
    public int Order => 1;

    public async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var seeds = SeedDataLoader.GetSeedDataFromResource<DictionaryEntrySeed>();

        foreach (var seed in seeds)
        {
            var exists = await context.DictionaryEntries
                .AnyAsync(x =>
                    x.Headword == seed.Headword &&
                    x.PartOfSpeech == seed.PartOfSpeech &&
                    x.Source == seed.Source,
                    cancellationToken);

            if (exists)
            {
                logger.LogInformation(
                    "Dictionary entry for headword '{Headword}' already exists. Skipping seeding.", seed.Headword);
                continue;
            }

            var entryResult = DictionaryEntry.Create(
                seed.Headword,
                seed.PartOfSpeech,
                seed.Source);

            if (entryResult.IsFailure)
            {
                logger.LogError(
                    "Failed to create dictionary entry for headword '{Headword}': {ErrorMessage}", seed.Headword, entryResult.Error);
                continue;
            }

            var dictionaryEntry = entryResult.Value;

            foreach (var pronunciation in seed.Pronunciations)
            {
                var pronunciationResult = dictionaryEntry.AddPronunciation(
                     pronunciation.Ipa,
                     pronunciation.Accent,
                     pronunciation.AudioUrl,
                     pronunciation.AudioSource);

                if (pronunciationResult.IsFailure)
                {
                    logger.LogError(
                        "Failed to add pronunciation for headword '{Headword}': {ErrorMessage}", seed.Headword, pronunciationResult.Error);
                    continue;
                }
            }

            foreach (var definition in seed.Definitions)
            {
                var definitionResult = dictionaryEntry.AddDefinition(definition.Text, definition.Examples);
                if (definitionResult.IsFailure)
                {
                    logger.LogError(
                        "Failed to add definition for headword '{Headword}': {ErrorMessage}", seed.Headword, definitionResult.Error);
                    continue;
                }
            }

            foreach (var image in seed.Images)
            {
                var imageResult = dictionaryEntry.AddImage(image.Url, image.Source);
                if (imageResult.IsFailure)
                {
                    logger.LogError(
                        "Failed to add image for headword '{Headword}': {ErrorMessage}", seed.Headword, imageResult.Error);
                    continue;
                }
            }

            await context.DictionaryEntries.AddAsync(dictionaryEntry, cancellationToken);
        }
    }
}
