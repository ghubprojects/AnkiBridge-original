using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateIpa;

public sealed record GenerateIpaQuery(string Headword, Accent Accent) : IRequest<Result<string>>;
