using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;

public interface IPhraseIpaResolver
{
    /// <summary>
    /// Attempts to construct IPA for a multi-word phrase by looking up
    /// each constituent word individually. Returns null if any word fails.
    /// </summary>
    Task<string?> ResolveAsync(string phrase, Accent accent, CancellationToken ct = default);
}