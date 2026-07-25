using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Abstractions.Dictionary;

public interface IDictionaryEntryProvider
{
    Task<Result<IReadOnlyList<DictionaryEntryResult>>> LookupAsync(
        string headword,
        CancellationToken cancellationToken = default);
}
