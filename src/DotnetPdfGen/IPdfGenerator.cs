namespace DotnetPdfGen;

/// <summary>Generates PDF documents from <see cref="PdfTemplate"/>s and model data.</summary>
public interface IPdfGenerator
{
    /// <summary>
    /// Binds <paramref name="model"/> into the template and returns the complete HTML document
    /// that would be rendered. Useful for previewing and debugging templates.
    /// </summary>
    string ComposeHtml(PdfTemplate template, object? model);

    /// <summary>Generates a PDF and returns its bytes.</summary>
    Task<byte[]> GenerateAsync(
        PdfTemplate template, object? model, PdfOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Generates a PDF and writes it to <paramref name="output"/>.</summary>
    Task GenerateToStreamAsync(
        PdfTemplate template, object? model, Stream output, PdfOptions? options = null, CancellationToken cancellationToken = default);
}
