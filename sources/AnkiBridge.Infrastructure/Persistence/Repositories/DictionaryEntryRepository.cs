using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace AnkiBridge.Infrastructure.Persistence.Repositories;

public sealed class DictionaryEntryRepository(ApplicationDbContext context) : IDictionaryEntryRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task<bool> ExistsAsync(
        string headword,
        PartOfSpeech partOfSpeech,
        DictionarySource source,
        CancellationToken cancellationToken = default)
    {
        return await context.DictionaryEntries
            .AnyAsync(x =>
                x.Headword == headword &&
                x.PartOfSpeech == partOfSpeech &&
                x.Source == source,
                cancellationToken);
    }

    public void Add(DictionaryEntry entry) 
        => context.DictionaryEntries.Add(entry);
}
