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
                x.Source,
                x.Definitions
                    .OrderBy(d => d.OrderIndex)
                    .Select(d => new DictionaryEntryDetailDefinition(
                        d.Text,
                        d.Examples
                            .Select(e => new DictionaryEntryDetailExample(e.Text))
                            .ToList()))
                    .ToList(),
                x.Translations
                    .Select(t => new DictionaryEntryDetailTranslation(
                        t.Source,
                        t.Text))
                    .ToList(),
                x.Pronunciations
                    .Select(p => new DictionaryEntryDetailPronunciation(
                        p.Accent,
                        p.Ipa,
                        p.AudioSource,
                        p.AudioUrl))
                    .ToList(),
                x.Images.Select(i => new DictionaryEntryDetailImage(
                    i.Source,
                    i.Url))
                .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
