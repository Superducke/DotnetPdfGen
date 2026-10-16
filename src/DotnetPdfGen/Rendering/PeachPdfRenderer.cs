using PeachPDF;
using PeachPDF.Network;

namespace DotnetPdfGen.Rendering;

/// <summary>
/// An <see cref="IPdfRenderer"/> built on PeachPDF, a pure .NET HTML/CSS layout engine.
/// No browser or external process is required.
/// </summary>
/// <remarks>
/// <para>
/// Supports modern CSS including flexbox, <c>@page</c> rules with margin boxes (e.g. page numbers),
/// page breaks, <c>@font-face</c>, SVG and gradients. CSS <c>@page</c> rules take precedence over
/// the size and margins in <see cref="PdfOptions"/>.
/// </para>
/// <para>
/// Instances are thread-safe: each render uses its own PeachPDF generator.
/// </para>
/// </remarks>
public sealed class PeachPdfRenderer : IPdfRenderer
{
    // PeachPDF resolves relative URLs against the directory of the file a FileUriNetworkLoader is
    // given. The HTML itself is passed as a string, so this file name is never read.
    private const string PlaceholderDocumentName = "__dotnetpdfgen_template__.html";

    private readonly PeachPdfRendererOptions _options;
    private readonly Lazy<Task<byte[][]>> _fonts;

    /// <summary>Creates a renderer.</summary>
    public PeachPdfRenderer(PeachPdfRendererOptions? options = null)
    {
        _options = options ?? new PeachPdfRendererOptions();
        var fontFiles = _options.FontFiles.ToArray();
        _fonts = new Lazy<Task<byte[][]>>(() => Task.WhenAll(fontFiles.Select(path => File.ReadAllBytesAsync(path))));
    }

    /// <inheritdoc />
    public async Task RenderAsync(PdfRenderRequest request, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(output);
        cancellationToken.ThrowIfCancellationRequested();

        var generator = new PeachPDF.PdfGenerator();
        foreach (var (family, replacement) in _options.FontFamilyMappings)
        {
            generator.AddFontFamilyMapping(family, replacement);
        }

        foreach (var font in await _fonts.Value.ConfigureAwait(false))
        {
            await generator.AddFontFromStream(new MemoryStream(font, writable: false)).ConfigureAwait(false);
        }

        // PeachPDF exposes no cancellation, so the token is only observed between stages.
        var document = await generator.GeneratePdf(request.Html, CreateConfig(request)).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        // PeachPDF saves synchronously; buffer first so callers can pass streams that only allow
        // async writes, such as an ASP.NET Core response body.
        using var buffer = new MemoryStream();
        document.Save(buffer);
        buffer.Position = 0;
        await buffer.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    private PdfGenerateConfig CreateConfig(PdfRenderRequest request)
    {
        var options = request.Options;
        var (width, height) = options.GetPageDimensions();

        var config = new PdfGenerateConfig
        {
            ManualPageWidth = width,
            ManualPageHeight = height,
            PageOrientation = PeachPDF.PageOrientation.Portrait,
            MarginTop = ToWholePoints(options.Margins.Top),
            MarginRight = ToWholePoints(options.Margins.Right),
            MarginBottom = ToWholePoints(options.Margins.Bottom),
            MarginLeft = ToWholePoints(options.Margins.Left),
            AllowLocalFileAccess = _options.AllowLocalFileAccess ?? request.BasePath is not null,
        };

        if (request.BasePath is not null)
        {
            config.NetworkLoader = new FileUriNetworkLoader(
                Path.Combine(Path.GetFullPath(request.BasePath), PlaceholderDocumentName));
        }

        if (options.Title is not null || options.Author is not null)
        {
            config.Metadata = new PdfDocumentMetadata { Title = options.Title, Author = options.Author };
        }

        return config;
    }

    private static int ToWholePoints(double points) => (int)Math.Round(points, MidpointRounding.AwayFromZero);
}
