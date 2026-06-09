using AnkiBridge.Application.Common.Contracts.Speech;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateAudio;

public sealed class GenerateAudioQueryHandler(ISpeechSynthesizer speechSynthesizer)
    : IRequestHandler<GenerateAudioQuery, Result<string>>
{
    public Task<Result<string>> Handle(GenerateAudioQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Headword))
            return Task.FromResult(Result.Failure<string>("Headword must not be empty."));

        var url = speechSynthesizer.BuildAudioUrl(request.Headword.Trim());

        return Task.FromResult(Result.Success(url));
    }
}