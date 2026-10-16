using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DotnetPdfGen.Web.Tests;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Lists_bundled_samples()
    {
        var samples = await _client.GetFromJsonAsync<JsonElement[]>("/api/samples");

        var names = samples!.Select(s => s.GetProperty("name").GetString()).ToArray();
        Assert.Equal(["certificate", "invoice", "report"], names.Order());
    }

    [Fact]
    public async Task Returns_sample_source_for_the_editor()
    {
        var sample = await _client.GetFromJsonAsync<JsonElement>("/api/samples/invoice");

        Assert.Contains("{{#each Lines}}", sample.GetProperty("html").GetString());
        Assert.Contains("@page", sample.GetProperty("css").GetString());
        Assert.Equal("INV-2026-0042", sample.GetProperty("data").GetProperty("Number").GetString());
    }

    [Theory]
    [InlineData("invoice", 1)]
    [InlineData("report", 2)]
    [InlineData("certificate", 1)]
    public async Task Renders_each_sample(string name, int expectedPages)
    {
        var pdf = await GetPdf($"/api/samples/{name}/pdf");

        Assert.Equal(expectedPages, PageCount(pdf));
    }

    [Fact]
    public async Task Certificate_is_landscape_from_its_css_page_rule()
    {
        var (width, height) = MediaBox(await GetPdf("/api/samples/certificate/pdf"));

        Assert.True(width > height, $"Expected landscape but MediaBox was {width}x{height}");
    }

    [Fact]
    public async Task Unknown_sample_is_not_found()
    {
        var response = await _client.GetAsync("/api/samples/does-not-exist/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Download_flag_sets_attachment_disposition()
    {
        var response = await _client.GetAsync("/api/samples/invoice/pdf?download=true");

        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("invoice.pdf", response.Content.Headers.ContentDisposition?.FileNameStar);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Renders_typed_csharp_invoice(bool paid)
    {
        var pdf = await GetPdf($"/api/examples/invoice?paid={paid}");

        Assert.Equal(1, PageCount(pdf));
    }

    [Fact]
    public async Task Renders_posted_template()
    {
        var response = await _client.PostAsJsonAsync("/api/pdf", new
        {
            html = "<h1>{{Title}}</h1>",
            data = new { Title = "Posted" },
            page = new { size = "Letter", orientation = "Landscape", marginMm = 10 },
        });

        response.EnsureSuccessStatusCode();
        var pdf = Encoding.Latin1.GetString(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal((792, 612), MediaBox(pdf));
        Assert.True(response.Headers.Contains("X-Render-Time-Ms"));
    }

    [Fact]
    public async Task Returns_bound_html()
    {
        var response = await _client.PostAsJsonAsync("/api/pdf/html", new
        {
            html = "<p>Hello {{Name}} <b>{{Tag}}</b></p>",
            css = "p { color: red; }",
            data = new { Name = "Ada", Tag = "<script>" },
        });

        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<p>Hello Ada <b>&lt;script&gt;</b></p>", html);
        Assert.Contains("p { color: red; }", html);
    }

    [Fact]
    public async Task Invalid_template_returns_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/pdf", new { html = "{{#each Items}}never closed" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("The template could not be bound.", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Unknown_page_size_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/pdf", new { html = "<p/>", page = new { size = "B9" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("page.size", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Posted_templates_cannot_read_local_files()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotnetpdfgen-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));
        try
        {
            var response = await _client.PostAsJsonAsync("/api/pdf", new { html = $"<img src=\"{new Uri(path).AbsoluteUri}\">" });

            response.EnsureSuccessStatusCode();
            var pdf = Encoding.Latin1.GetString(await response.Content.ReadAsByteArrayAsync());
            Assert.DoesNotMatch(@"/Subtype\s*/Image", pdf);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Serves_preview_page()
    {
        var html = await _client.GetStringAsync("/");

        Assert.Contains("DotnetPdfGen Playground", html);
    }

    private async Task<string> GetPdf(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var pdf = Encoding.Latin1.GetString(await response.Content.ReadAsByteArrayAsync());
        Assert.StartsWith("%PDF-", pdf);
        return pdf;
    }

    private static int PageCount(string pdf) => Regex.Count(pdf, @"/Type\s*/Page(?![a-zA-Z])");

    private static (int Width, int Height) MediaBox(string pdf)
    {
        var match = Regex.Match(pdf, @"/MediaBox\s*\[\s*0\s+0\s+([\d.]+)\s+([\d.]+)\s*\]");
        Assert.True(match.Success, "No /MediaBox found");
        return ((int)Math.Round(double.Parse(match.Groups[1].Value)), (int)Math.Round(double.Parse(match.Groups[2].Value)));
    }
}
