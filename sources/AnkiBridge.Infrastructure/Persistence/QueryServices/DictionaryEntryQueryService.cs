using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices;
using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace AnkiBridge.Infrastructure.Persistence.QueryServices;

public sealed class DictionaryEntryQueryService(ApplicationDbContext context) : IDictionaryEntryQueryService
{
    public async Task<IReadOnlyList<DictionaryEntrySearchResult>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken)
    {
        return await context.DictionaryEntries
            .AsNoTracking()
            .Where(x => EF.Functions.Like(x.Headword, $"%{keyword}%"))
            .OrderBy(x => x.Headword)
            .Select(x => new DictionaryEntrySearchResult(
                x.Id,
                x.Headword,
                x.PartOfSpeech))
            .ToListAsync(cancellationToken);
    }

    public async Task<DictionaryEntryDetail?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.DictionaryEntries
            .AsNoTracking()
            .Include(e => e.Pronunciations)
            .Include(e => e.Definitions)
                .ThenInclude(d => d.Examples)
            .Include(e => e.Images)
            .AsSplitQuery()
            .Where(x => x.Id == id)
            .Select(x => new DictionaryEntryDetail(
                x.Id,
                x.Headword,
                x.PartOfSpeech,
                x.Pronunciations
                    .Select(p => new DictionaryEntryDetailPronunciation(
                        p.Accent,
                        p.Ipa,
                        p.AudioUrl))
                    .ToList(),
                x.Definitions
                    .OrderBy(d => d.OrderIndex)
                    .Select(d => new DictionaryEntryDetailDefinition(
                        d.Text,
                        d.Examples.Select(e => e.Text).ToList()))
                    .ToList(),
                x.Images.Select(i => i.Url).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
