using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.ScrapeDictionaryEntry;

public sealed record ScrapeDictionaryEntryCommand(string Headword) : IRequest<Result>;
