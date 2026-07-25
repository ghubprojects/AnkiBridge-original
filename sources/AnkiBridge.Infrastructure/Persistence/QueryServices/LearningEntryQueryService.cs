using AnkiBridge.Application.Common.Query.Pagination;
using AnkiBridge.Application.Features.Learning.Contracts.QueryServices;
using AnkiBridge.Application.Features.Learning.Contracts.QueryServices.Models;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using AnkiBridge.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AnkiBridge.Infrastructure.Persistence.QueryServices;

public sealed class LearningEntryQueryService(
    ApplicationDbContext context)
    : ILearningEntryQueryService
{
    public async Task<PaginatedResult<LearningEntrySearchResult>> SearchAsync(
        string? keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.LearningEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(x => EF.Functions.Like(x.Headword, $"%{keyword}%"));

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToPaginatedResultAsync(
                pageNumber,
                pageSize,
                x => new LearningEntrySearchResult(
                    x.Id,
                    x.Headword,
                    x.PartOfSpeech,
                    x.Ipa,
                    x.Translation,
                    x.AudioUploadStatus,
                    x.ImageUploadStatus,
                    x.CreatedAt),
                cancellationToken);
    }
}
