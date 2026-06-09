using AnkiBridge.Domain.Aggregates.Learning;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Infrastructure.Persistence.Abstractions;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using AnkiBridge.Infrastructure.Persistence.Seeding.Helpers;
using AnkiBridge.Infrastructure.Persistence.Seeding.Models;

namespace AnkiBridge.Infrastructure.Persistence.Seeding.Seeders;

public sealed class LearningSeeder : IDbSeeder
{
    public int Order => 2;

    public async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var seeds = SeedDataLoader.GetSeedDataFromResource<LearningEntrySeed>();

        foreach (var seed in seeds)
        {
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
                seed.Ipa
            );

            if (result.IsFailure)
            {
                // Bạn có thể log lỗi ở đây nếu dữ liệu seed JSON có vấn đề
                continue;
            }

            var learningEntry = result.Value;

            // 2. Sử dụng các phương thức Domain mới để mapping chính xác Media Path và Source ban đầu
            if (!string.IsNullOrWhiteSpace(seed.AudioPath))
            {
                learningEntry.SetAudioFromDictionary(seed.AudioPath);
            }

            if (!string.IsNullOrWhiteSpace(seed.ImagePath))
            {
                learningEntry.SetImageFromDictionary(seed.ImagePath);
            }

            await context.LearningEntries.AddAsync(learningEntry, cancellationToken);
        }
    }
}