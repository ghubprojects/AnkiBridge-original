using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public interface IDictionaryEntryRepository : IRepository<DictionaryEntry, Guid>
{
    Task<DictionaryEntry?> FindByHeadwordAsync(
        string headword,
        PartOfSpeech partOfSpeech,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DictionaryEntry entry,
        CancellationToken cancellationToken);
}