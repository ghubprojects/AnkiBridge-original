using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices;

public interface IDictionaryEntryQueryService
{
    Task<IReadOnlyList<DictionaryEntrySearchResult>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken);

    Task<DictionaryEntryDetail?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);
}
