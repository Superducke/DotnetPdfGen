using System.Text;
using System.Text.RegularExpressions;
using DotnetPdfGen.Rendering;
using DotnetPdfGen.Templating;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetPdfGen.Tests.Rendering;

public sealed class PeachPdfRendererTests : IDisposable
{
    // A 1x1 PNG, used to detect whether an <img> was loaded (embedded PNGs become /Subtype /Image).
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly string _assetDir = Directory.CreateTempSubdirectory("dotnetpdfgen-tests-").FullName;

    public PeachPdfRendererTests()
    {
        File.WriteAllBytes(Path.Combine(_assetDir, "pixel.png"), Png);
    }

    public void Dispose() => Directory.Delete(_assetDir, recursive: true);

    [Fact]
    public async Task Renders_a_valid_pdf()
    {
        var pdf = await Render("<h1>Hello</h1>");

        Assert.StartsWith("%PDF-", pdf);
        Assert.Contains("%%EOF", pdf);
        Assert.Equal(1, PageCount(pdf));
    }

    [Theory]
    [InlineData(PageOrientation.Portrait, 612, 792)]
    [InlineData(PageOrientation.Landscape, 792, 612)]
    public async Task Applies_page_size_and_orientation(PageOrientation orientation, int width, int height)
    {
        var pdf = await Render("<p>x</p>", new PdfOptions { PageSize = PageSize.Letter, Orientation = orientation });

        Assert.Equal((width, height), MediaBox(pdf));
    }

    [Fact]
    public async Task Css_page_rule_overrides_options()
    {
        var pdf = await Render("<html><head><style>@page { size: 200pt 300pt; }</style></head><body>x</body></html>");

        Assert.Equal((200, 300), MediaBox(pdf));
    }

    [Fact]
    public async Task Honors_css_page_breaks()
    {
        var pdf = await Render("<p>One</p><p style=\"page-break-before: always\">Two</p>");

        Assert.Equal(2, PageCount(pdf));
    }

    [Fact]
    public async Task Resolves_relative_resources_against_base_path()
    {
        var pdf = await Render("<img src=\"pixel.png\">", basePath: _assetDir);

        Assert.True(ContainsImage(pdf));
    }

    [Fact]
    public async Task Refuses_local_files_without_base_path()
    {
        var pdf = await Render($"<img src=\"{FileUri("pixel.png")}\">");

        Assert.False(ContainsImage(pdf));
    }

    [Fact]
    public async Task Allows_local_files_when_explicitly_enabled()
    {
        var renderer = new PeachPdfRenderer(new PeachPdfRendererOptions { AllowLocalFileAccess = true });

        var pdf = await Render($"<img src=\"{FileUri("pixel.png")}\">", renderer: renderer);

        Assert.True(ContainsImage(pdf));
    }

    [Fact]
    public async Task Data_uris_always_load()
    {
        var pdf = await Render($"<img src=\"data:image/png;base64,{Convert.ToBase64String(Png)}\">");

        Assert.True(ContainsImage(pdf));
    }

    [Fact]
    public async Task Writes_to_streams_that_reject_synchronous_io()
    {
        using var output = new AsyncOnlyStream();

        await new PeachPdfRenderer().RenderAsync(new PdfRenderRequest(Document("<p>x</p>"), PdfOptions.Default), output);

        Assert.StartsWith("%PDF-", Encoding.Latin1.GetString(output.ToArray()));
    }

    [Fact]
    public async Task Throws_when_cancelled_before_rendering()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PeachPdfRenderer().RenderAsync(new PdfRenderRequest("<p/>", PdfOptions.Default), Stream.Null, cts.Token));
    }

    [Fact]
    public async Task End_to_end_through_dependency_injection()
    {
        using var provider = new ServiceCollection()
            .AddDotnetPdfGen(o => o.Binding.StrictMode = true)
            .BuildServiceProvider();
        var generator = provider.GetRequiredService<IPdfGenerator>();
        var template = new PdfTemplate(
            "{{#each Pages}}<section style=\"page-break-after: always\">{{Title}}</section>{{/each}}<p>End</p>",
            "section { font-size: 20pt; }");

        var bytes = await generator.GenerateAsync(template, new { Pages = new[] { new { Title = "A" }, new { Title = "B" } } });

        Assert.Equal(3, PageCount(Encoding.Latin1.GetString(bytes)));
        Assert.Throws<TemplateBindingException>(() => generator.ComposeHtml(template, new { }));
    }

    private async Task<string> Render(
        string body, PdfOptions? options = null, string? basePath = null, PeachPdfRenderer? renderer = null)
    {
        using var output = new MemoryStream();
        var request = new PdfRenderRequest(Document(body), options ?? PdfOptions.Default, basePath);
        await (renderer ?? new PeachPdfRenderer()).RenderAsync(request, output);
        return Encoding.Latin1.GetString(output.ToArray());
    }

    private static string Document(string body) => HtmlDocumentComposer.Compose(body, css: null);

    private string FileUri(string fileName) => new Uri(Path.Combine(_assetDir, fileName)).AbsoluteUri;

    private static int PageCount(string pdf) => Regex.Count(pdf, @"/Type\s*/Page(?![a-zA-Z])");

    private static bool ContainsImage(string pdf) => Regex.IsMatch(pdf, @"/Subtype\s*/Image");

    private static (int Width, int Height) MediaBox(string pdf)
    {
        var match = Regex.Match(pdf, @"/MediaBox\s*\[\s*0\s+0\s+([\d.]+)\s+([\d.]+)\s*\]");
        Assert.True(match.Success, "No /MediaBox found");
        return ((int)Math.Round(double.Parse(match.Groups[1].Value)), (int)Math.Round(double.Parse(match.Groups[2].Value)));
    }

    /// <summary>Mimics ASP.NET Core's response body, which rejects synchronous writes by default.</summary>
    private sealed class AsyncOnlyStream : Stream
    {
        private readonly MemoryStream _inner = new();

        public byte[] ToArray() => _inner.ToArray();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("Synchronous operations are disallowed.");

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.WriteAsync(buffer, cancellationToken);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _inner.WriteAsync(buffer, offset, count, cancellationToken);

        public override void Flush() => throw new InvalidOperationException("Synchronous operations are disallowed.");
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
