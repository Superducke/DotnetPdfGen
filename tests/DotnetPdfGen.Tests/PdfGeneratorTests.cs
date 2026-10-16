using System.Text;
using DotnetPdfGen.Rendering;
using DotnetPdfGen.Templating;

namespace DotnetPdfGen.Tests;

public class PdfGeneratorTests
{
    private readonly RecordingRenderer _renderer = new();
    private readonly PdfGenerator _generator;

    public PdfGeneratorTests()
    {
        _generator = new PdfGenerator(new HandlebarsTemplateBinder(), _renderer);
    }

    [Fact]
    public void ComposeHtml_binds_model_and_injects_css()
    {
        var template = new PdfTemplate("<h1>{{Title}}</h1>", "h1 { font-size: 20pt; }");

        var html = _generator.ComposeHtml(template, new { Title = "Quarterly Report" });

        Assert.Contains("<h1>Quarterly Report</h1>", html);
        Assert.Contains("h1 { font-size: 20pt; }", html);
    }

    [Fact]
    public void Css_is_not_data_bound()
    {
        var template = new PdfTemplate("<p></p>", "/* {{Title}} */");

        var html = _generator.ComposeHtml(template, new { Title = "x" });

        Assert.Contains("/* {{Title}} */", html);
    }

    [Fact]
    public async Task GenerateAsync_passes_composed_html_options_and_base_path_to_renderer()
    {
        var template = new PdfTemplate("<p>{{Name}}</p>") { BasePath = "C:/templates" };
        var options = new PdfOptions { PageSize = PageSize.Letter, Orientation = PageOrientation.Landscape };

        var bytes = await _generator.GenerateAsync(template, new { Name = "Ada" }, options);

        Assert.Equal("PDF", Encoding.ASCII.GetString(bytes));
        var request = Assert.Single(_renderer.Requests);
        Assert.Contains("<p>Ada</p>", request.Html);
        Assert.Same(options, request.Options);
        Assert.Equal("C:/templates", request.BasePath);
    }

    [Fact]
    public async Task GenerateAsync_uses_default_options_when_none_given()
    {
        await _generator.GenerateAsync(new PdfTemplate("<p/>"), null);

        Assert.Same(PdfOptions.Default, Assert.Single(_renderer.Requests).Options);
    }

    [Fact]
    public async Task GenerateToStreamAsync_writes_to_caller_stream()
    {
        using var output = new MemoryStream();

        await _generator.GenerateToStreamAsync(new PdfTemplate("<p/>"), null, output);

        Assert.Equal("PDF", Encoding.ASCII.GetString(output.ToArray()));
    }

    private sealed class RecordingRenderer : IPdfRenderer
    {
        public List<PdfRenderRequest> Requests { get; } = [];

        public Task RenderAsync(PdfRenderRequest request, Stream output, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return output.WriteAsync("PDF"u8.ToArray(), cancellationToken).AsTask();
        }
    }
}
