using AngleSharp.Dom;

namespace AnkiBridge.Infrastructure.Extensions;

internal static class AngleSharpQueryExtensions
{
    /// <summary>
    /// Returns the first element matched by the first applicable selector.
    /// Selectors are evaluated in the order provided.
    /// </summary>
    public static IElement? QueryFirst(this IParentNode node, IReadOnlyList<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var element = node.QuerySelector(selector);

            if (element is not null)
                return element;
        }

        return null;
    }

    /// <summary>
    /// Returns the first non-empty trimmed text matched by the first applicable selector.
    /// Selectors are evaluated in the order provided.
    /// </summary>
    public static string? QueryFirstText(this IParentNode node, IReadOnlyList<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var text = node.QuerySelector(selector)?.TextContent.Trim();

            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }

    /// <summary>
    /// Returns all elements matched by the first selector that produces any results.
    /// Selectors are evaluated in the order provided.
    /// </summary>
    public static IReadOnlyList<IElement> QueryAll(this IParentNode node, IReadOnlyList<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var elements = node.QuerySelectorAll(selector);

            if (elements.Length > 0)
                return elements;
        }

        return [];
    }
}
