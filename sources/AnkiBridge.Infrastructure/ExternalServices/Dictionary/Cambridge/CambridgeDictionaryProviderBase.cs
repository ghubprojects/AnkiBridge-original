using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AnkiBridge.Shared.Results;
using Microsoft.Extensions.Options;

namespace AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;

public abstract class CambridgeDictionaryProviderBase(IOptions<CambridgeDictionaryOptions> options)
{
    private readonly CambridgeDictionaryOptions _options = options.Value;
    private readonly Uri _baseUri = new(options.Value.BaseUrl, UriKind.Absolute);

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    protected async Task<Result<TResult>> FetchAndParseAsync<TResult>(
        string url,
        Func<IDocument, Result<TResult>> parse,
        CancellationToken cancellationToken)
    {
        try
        {
            using var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
            var document = await context.OpenAsync(url, cancellationToken);
            return parse(document);
        }
        catch (Exception)
        {
            return Result.Failure<TResult>("Cambridge Dictionary is unavailable.");
        }
    }

    protected string BuildUrl(string section, string word) =>
        $"{_options.BaseUrl.TrimEnd('/')}/{section}/{Slugify(word)}";

    protected string ToAbsoluteUrl(string value) =>
        new Uri(_baseUri, value).AbsoluteUri;

    protected static string CleanText(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : WhitespaceRegex.Replace(text, " ").Trim();

    private static string Slugify(string word) =>
        Uri.EscapeDataString(word.Trim().ToLowerInvariant().Replace(' ', '-'));
}
