using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.LookupDictionaryEntries;

public sealed record LookupDictionaryEntriesCommand(string Headword) : IRequest<Result>;
