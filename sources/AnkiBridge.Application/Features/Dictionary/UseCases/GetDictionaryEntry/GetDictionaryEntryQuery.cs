using AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GetDictionaryEntry;

public sealed record GetDictionaryEntryQuery(Guid Id) : IRequest<Result<DictionaryEntryDetail>>;
