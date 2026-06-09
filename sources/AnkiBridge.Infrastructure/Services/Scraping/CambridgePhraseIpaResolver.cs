using AnkiBridge.Application.Features.Dictionary.Contracts.Scraping;
using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Infrastructure.Services.Scraping;

public sealed class CambridgePhraseIpaResolver(IDictionaryProvider provider) : IPhraseIpaResolver
{
    public async Task<string?> ResolveAsync(string phrase, Accent accent, CancellationToken ct = default)
    {
        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1) return null; // single word — caller should already have IPA

        var ipaParts = new List<string>(words.Length);

        foreach (var word in words)
        {
            var result = await provider.ScrapeAsync(word, ct);

            var ipa = result.IsSuccess
                ? result.Value
                    .SelectMany(e => e.Pronunciations)
                    .FirstOrDefault(p => p.Accent == accent)?.Ipa
                : null;

            if (string.IsNullOrWhiteSpace(ipa)) return null; // bail — partial IPA is misleading

            ipaParts.Add(ipa);
        }

        return string.Join(" ", ipaParts);
    }
}