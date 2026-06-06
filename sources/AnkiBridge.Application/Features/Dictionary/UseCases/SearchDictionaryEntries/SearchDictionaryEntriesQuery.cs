using AnkiBridge.Application.Common.Query.Pagination;
using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.SearchDictionaryEntries;

public sealed record SearchDictionaryEntriesQuery(
    string Keyword
) : IRequest<Result<IReadOnlyList<DictionaryEntrySearchResult>>>;
