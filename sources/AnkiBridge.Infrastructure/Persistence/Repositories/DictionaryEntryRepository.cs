using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;

namespace AnkiBridge.Infrastructure.Persistence.Repositories;

public sealed class DictionaryEntryRepository(ApplicationDbContext context) : IDictionaryEntryRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task AddAsync(DictionaryEntry entry, CancellationToken cancellationToken)
    {
        context.DictionaryEntries.Add(entry);
    }

    public Task<DictionaryEntry?> FindByHeadwordAsync(string headword, PartOfSpeech partOfSpeech, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
