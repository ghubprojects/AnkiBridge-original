using AnkiBridge.Application.Abstractions.Speech;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateAudio;

public sealed class GenerateAudioQueryHandler(IAudioProvider audioProvider)
    : IRequestHandler<GenerateAudioQuery, Result<AudioResult>>
{
    public async Task<Result<AudioResult>> Handle(GenerateAudioQuery query, CancellationToken cancellationToken) =>
        audioProvider.Synthesize(query.Text.Trim());
}
