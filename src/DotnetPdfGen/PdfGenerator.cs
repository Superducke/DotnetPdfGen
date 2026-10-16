using DotnetPdfGen.Rendering;
using DotnetPdfGen.Templating;

namespace DotnetPdfGen;

/// <summary>
/// The default <see cref="IPdfGenerator"/>: binds the model into the HTML, injects the CSS,
/// and hands the resulting document to an <see cref="IPdfRenderer"/>.
/// </summary>
public sealed class PdfGenerator : IPdfGenerator
{
    private readonly ITemplateBinder _binder;
    private readonly IPdfRenderer _renderer;

    /// <summary>Creates a generator.</summary>
    public PdfGenerator(ITemplateBinder binder, IPdfRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(renderer);
        _binder = binder;
        _renderer = renderer;
    }

    /// <inheritdoc />
    public string ComposeHtml(PdfTemplate template, object? model)
    {
        ArgumentNullException.ThrowIfNull(template);

        var boundHtml = _binder.Bind(template.Html, model);
        return HtmlDocumentComposer.Compose(boundHtml, template.Css);
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateAsync(
        PdfTemplate template, object? model, PdfOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await GenerateToStreamAsync(template, model, buffer, options, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <inheritdoc />
    public Task GenerateToStreamAsync(
        PdfTemplate template, object? model, Stream output, PdfOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);

        var html = ComposeHtml(template, model);
        var request = new PdfRenderRequest(html, options ?? PdfOptions.Default, template.BasePath);
        return _renderer.RenderAsync(request, output, cancellationToken);
    }
}
