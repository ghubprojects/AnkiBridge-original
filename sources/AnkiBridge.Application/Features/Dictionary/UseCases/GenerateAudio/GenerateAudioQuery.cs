using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateAudio;

public sealed record GenerateAudioQuery(string Headword) : IRequest<Result<string>>;