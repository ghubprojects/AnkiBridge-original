using AnkiBridge.Application.Common.Query.Pagination;
using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices;
using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.SearchDictionaryEntries;

public sealed class SearchDictionaryEntriesQueryHandler(
    IDictionaryEntryQueryService queryService)
    : IRequestHandler<SearchDictionaryEntriesQuery, Result<IReadOnlyList<DictionaryEntrySearchResult>>>
{
    public async Task<Result<IReadOnlyList<DictionaryEntrySearchResult>>> Handle(SearchDictionaryEntriesQuery request, CancellationToken cancellationToken)
    {
        var entries = await queryService.SearchAsync(request.Keyword, cancellationToken);

        return Result.Success(entries);
    }
}