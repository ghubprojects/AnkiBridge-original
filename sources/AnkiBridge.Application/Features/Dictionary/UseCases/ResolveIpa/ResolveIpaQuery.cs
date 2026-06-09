using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.ResolveIpa;

public sealed record ResolveIpaQuery(string Headword, Accent Accent) : IRequest<Result<string>>;