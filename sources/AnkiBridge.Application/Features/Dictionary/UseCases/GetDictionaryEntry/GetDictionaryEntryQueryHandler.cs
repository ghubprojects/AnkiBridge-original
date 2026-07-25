using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices;
using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GetDictionaryEntry;

public sealed class GetDictionaryEntryQueryHandler(IDictionaryEntryQueryService queryService)
    : IRequestHandler<GetDictionaryEntryQuery, Result<DictionaryEntryDetail>>
{
    public async Task<Result<DictionaryEntryDetail>> Handle(GetDictionaryEntryQuery request, CancellationToken cancellationToken)
    {
        var entry = await queryService.GetAsync(request.Id, cancellationToken);
        if (entry is null)
            return Result.Failure<DictionaryEntryDetail>("Dictionary entry not found.");

        return entry;
    }
}
