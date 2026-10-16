namespace DotnetPdfGen.Rendering;

/// <summary>
/// Converts a complete, already-bound HTML document into PDF bytes.
/// </summary>
/// <remarks>
/// Implementations are engine-specific. Keeping rendering behind this interface lets the
/// templating and public API stay the same if the rendering engine is swapped.
/// </remarks>
public interface IPdfRenderer
{
    /// <summary>Renders <paramref name="request"/> as a PDF and writes it to <paramref name="output"/>.</summary>
    Task RenderAsync(PdfRenderRequest request, Stream output, CancellationToken cancellationToken = default);
}

/// <summary>The input to an <see cref="IPdfRenderer"/>.</summary>
/// <param name="Html">The complete HTML document, with data already bound and CSS already injected.</param>
/// <param name="Options">Page setup.</param>
/// <param name="BasePath">Optional base directory for resolving relative resources.</param>
public sealed record PdfRenderRequest(string Html, PdfOptions Options, string? BasePath = null);
