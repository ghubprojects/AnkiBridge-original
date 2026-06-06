using AnkiBridge.Domain.Aggregates.Flashcard.Notes;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace AnkiBridge.Infrastructure.Persistence.Repositories;

public sealed class NoteRepository(ApplicationDbContext context) : INoteRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task<Note?> GetByIdAsync(
        Guid id,
        bool includeRelated = false,
        CancellationToken cancellationToken = default)
    {
        var query = context.Notes.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(x => x.LearningEntry)
                .Include(x => x.Deck)
                .Include(x => x.NoteType)
                .AsSplitQuery();
        }

        return await query
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Note>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        bool includeRelated = false,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        var query = context.Notes
            .Where(x => ids.Contains(x.Id))
            .AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(x => x.LearningEntry)
                    .ThenInclude(le => le.Examples)
                .Include(x => x.Deck)
                .Include(x => x.NoteType)
                .AsSplitQuery();
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.Notes
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid learningEntryId,
        Guid noteTypeId,
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        return await context.Notes
            .AnyAsync(x =>
                x.LearningEntryId == learningEntryId &&
                x.NoteTypeId == noteTypeId &&
                x.DeckId == deckId,
                cancellationToken);
    }

    public async Task AddAsync(
        Note note,
        CancellationToken cancellationToken = default)
    {
        context.Notes.Add(note);
    }

    public async Task AddRangeAsync(
        IEnumerable<Note> notes,
        CancellationToken cancellationToken = default)
    {
        context.Notes.AddRange(notes);
    }
}
