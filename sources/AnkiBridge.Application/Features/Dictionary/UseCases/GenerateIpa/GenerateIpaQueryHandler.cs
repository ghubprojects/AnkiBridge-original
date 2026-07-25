using AnkiBridge.Application.Abstractions.Dictionary;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateIpa;

public sealed class GenerateIpaQueryHandler(IIpaProvider phraseIpaProvider)
    : IRequestHandler<GenerateIpaQuery, Result<string>>
{
    public Task<Result<string>> Handle(GenerateIpaQuery request, CancellationToken cancellationToken)=>
        phraseIpaProvider.TranscribeAsync(
            request.Headword.Trim(),
            request.Accent,
            cancellationToken);
}
