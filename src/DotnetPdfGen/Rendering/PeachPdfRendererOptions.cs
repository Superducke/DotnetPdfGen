namespace DotnetPdfGen.Rendering;

/// <summary>Options for <see cref="PeachPdfRenderer"/>.</summary>
public sealed class PeachPdfRendererOptions
{
    /// <summary>
    /// Paths to TrueType, OpenType, WOFF or WOFF2 font files to make available to every render,
    /// in addition to fonts installed on the operating system.
    /// </summary>
    public IList<string> FontFiles { get; } = new List<string>();

    /// <summary>
    /// Font family substitutions, e.g. <c>["Segoe UI"] = "Arial"</c>. Useful for rendering templates
    /// on servers that lack the fonts they were designed with.
    /// </summary>
    public IDictionary<string, string> FontFamilyMappings { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether templates may load resources from the local file system.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/> (the default), local file access is allowed only for templates
    /// that set <see cref="PdfTemplate.BasePath"/>, which are assumed to be trusted. Without a base path,
    /// <c>file:</c> references and relative paths are refused, so HTML from untrusted sources
    /// cannot read files from the server. <c>data:</c> URIs always work.
    /// </remarks>
    public bool? AllowLocalFileAccess { get; set; }
}
