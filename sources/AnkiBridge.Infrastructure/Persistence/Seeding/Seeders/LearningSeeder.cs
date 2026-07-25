using AnkiBridge.Domain.Aggregates.Learning;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.Persistence.Abstractions;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using AnkiBridge.Infrastructure.Persistence.Seeding.Helpers;
using AnkiBridge.Infrastructure.Persistence.Seeding.Models;
using Microsoft.EntityFrameworkCore;

namespace AnkiBridge.Infrastructure.Persistence.Seeding.Seeders;

public sealed class LearningSeeder : IDbSeeder
{
    public int Order => 2;

    public async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var seeds = SeedDataLoader.GetSeedDataFromResource<LearningEntrySeed>();

        foreach (var seed in seeds)
        {
            var exists = await context.LearningEntries
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.Headword == seed.Headword
                    && x.PartOfSpeech == seed.PartOfSpeech
                    && x.Accent == seed.Accent
                    && x.Definition == seed.Definition,
                    cancellationToken);

            if (exists)
                continue;

            // 1. Gọi hàm Create theo đúng thứ tự tham số mới của Domain
            var result = LearningEntry.Create(
                seed.DictionaryEntryId,
                seed.Headword,
                seed.PartOfSpeech,
                seed.Cloze,
                seed.Definition,
                seed.Examples,
                TranslationSource.Google, // Mặc định nguồn dịch cho dữ liệu seed
                seed.Translation,
                seed.Accent,
                seed.Ipa,
                string.IsNullOrWhiteSpace(seed.AudioPath) ? null : AudioSource.Unknown,
                seed.AudioPath,
                string.IsNullOrWhiteSpace(seed.ImagePath) ? null : ImageSource.Unknown,
                seed.ImagePath
            );

            if (result.IsFailure)
            {
                // Bạn có thể log lỗi ở đây nếu dữ liệu seed JSON có vấn đề
                continue;
            }

            await context.LearningEntries.AddAsync(result.Value, cancellationToken);
        }
    }
}
