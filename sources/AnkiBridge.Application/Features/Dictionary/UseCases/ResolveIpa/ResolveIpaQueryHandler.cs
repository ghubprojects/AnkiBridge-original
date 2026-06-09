using AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.ResolveIpa;

public sealed class ResolveIpaQueryHandler(IPhraseIpaResolver phraseIpaResolver)
    : IRequestHandler<ResolveIpaQuery, Result<string>>
{
    public async Task<Result<string>> Handle(ResolveIpaQuery request, CancellationToken cancellationToken)
    {
        var ipa = await phraseIpaResolver.ResolveAsync(
            request.Headword.Trim(),
            request.Accent,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(ipa))
            return Result.Failure<string>($"Could not resolve IPA for \"{request.Headword}\".");

        return ipa;
    }
}