using System.Text.RegularExpressions;

namespace DotnetPdfGen;

/// <summary>
/// Produces a complete HTML document from bound HTML and optional CSS.
/// </summary>
internal static partial class HtmlDocumentComposer
{
    public static string Compose(string html, string? css)
    {
        var style = string.IsNullOrWhiteSpace(css) ? string.Empty : $"<style>\n{css}\n</style>\n";

        var headClose = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        if (headClose >= 0)
        {
            return html.Insert(headClose, style);
        }

        var htmlOpen = HtmlOpenTag().Match(html);
        if (htmlOpen.Success)
        {
            return html.Insert(htmlOpen.Index + htmlOpen.Length, $"<head>\n{style}</head>\n");
        }

        // A fragment: wrap it in a full document so every renderer receives the same shape.
        var body = BodyOpenTag().IsMatch(html) ? html : $"<body>\n{html}\n</body>";
        return $"<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n{style}</head>\n{body}\n</html>";
    }

    [GeneratedRegex(@"<html\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlOpenTag();

    [GeneratedRegex(@"<body\b", RegexOptions.IgnoreCase)]
    private static partial Regex BodyOpenTag();
}
