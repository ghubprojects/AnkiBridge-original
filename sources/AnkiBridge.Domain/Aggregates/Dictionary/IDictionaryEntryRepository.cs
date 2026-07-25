using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public interface IDictionaryEntryRepository : IRepository<DictionaryEntry, Guid>
{
    Task<bool> ExistsAsync(
        string headword,
        PartOfSpeech partOfSpeech,
        DictionarySource source,
        CancellationToken cancellationToken = default);

    void Add(DictionaryEntry entry);
}
