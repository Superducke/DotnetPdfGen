namespace DotnetPdfGen;

/// <summary>
/// Describes the layout of a PDF document as HTML with optional CSS.
/// </summary>
/// <remarks>
/// The <see cref="Html"/> may contain binding expressions such as <c>{{Title}}</c>, which are
/// replaced with values from the model passed to <see cref="IPdfGenerator"/>. The <see cref="Css"/>
/// is injected verbatim into the document's <c>&lt;head&gt;</c> and is not data-bound.
/// </remarks>
public sealed record PdfTemplate
{
    /// <summary>Creates a template from HTML and optional CSS.</summary>
    /// <param name="html">The HTML template. May be a full document or a fragment.</param>
    /// <param name="css">Optional CSS to inject into the document head.</param>
    public PdfTemplate(string html, string? css = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        Html = html;
        Css = css;
    }

    /// <summary>The HTML template, including any <c>{{...}}</c> binding expressions.</summary>
    public string Html { get; init; }

    /// <summary>Optional CSS injected into the document head as a <c>&lt;style&gt;</c> block.</summary>
    public string? Css { get; init; }

    /// <summary>
    /// Optional base directory used to resolve relative resources such as images, stylesheets and fonts.
    /// </summary>
    /// <remarks>
    /// Setting a base path marks the template as trusted to read from the local file system.
    /// Leave it unset for HTML from untrusted sources.
    /// </remarks>
    public string? BasePath { get; init; }

    /// <summary>Loads a template from an HTML file and an optional CSS file.</summary>
    /// <remarks>
    /// <see cref="BasePath"/> is set to the HTML file's directory so relative resources resolve
    /// next to the template.
    /// </remarks>
    public static async Task<PdfTemplate> FromFilesAsync(
        string htmlPath, string? cssPath = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(htmlPath);

        var html = await File.ReadAllTextAsync(htmlPath, cancellationToken).ConfigureAwait(false);
        var css = cssPath is null
            ? null
            : await File.ReadAllTextAsync(cssPath, cancellationToken).ConfigureAwait(false);

        return new PdfTemplate(html, css)
        {
            BasePath = Path.GetDirectoryName(Path.GetFullPath(htmlPath)),
        };
    }
}
