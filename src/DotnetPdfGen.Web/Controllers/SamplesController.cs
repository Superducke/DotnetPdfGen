using System.Text.Json;
using DotnetPdfGen.Web.Examples;
using DotnetPdfGen.Web.Samples;
using Microsoft.AspNetCore.Mvc;

namespace DotnetPdfGen.Web.Controllers;

/// <summary>Lists and renders the bundled sample templates.</summary>
[ApiController]
[Route("api/samples")]
public sealed class SamplesController(SampleTemplateStore store, IPdfGenerator generator) : ControllerBase
{
    public sealed record SampleSummary(string Name, string Title, string Description);

    public sealed record SampleDetail(string Name, string Title, string Description, string Html, string? Css, JsonElement Data);

    [HttpGet]
    public IEnumerable<SampleSummary> List() =>
        store.All.Select(s => new SampleSummary(s.Name, s.Title, s.Description));

    /// <summary>Returns a sample's HTML, CSS and sample data, for loading into an editor.</summary>
    [HttpGet("{name}")]
    public async Task<ActionResult<SampleDetail>> Get(string name, CancellationToken cancellationToken)
    {
        if (store.Find(name) is not { } sample)
        {
            return NotFound();
        }

        var template = await store.LoadTemplateAsync(sample, cancellationToken);
        var data = await store.LoadSampleDataAsync(sample, cancellationToken);
        return new SampleDetail(sample.Name, sample.Title, sample.Description, template.Html, template.Css, data);
    }

    /// <summary>Renders a sample with its bundled sample data.</summary>
    [HttpGet("{name}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> Render(string name, bool download, CancellationToken cancellationToken)
    {
        if (store.Find(name) is not { } sample)
        {
            return NotFound();
        }

        var template = await store.LoadTemplateAsync(sample, cancellationToken);
        var data = await store.LoadSampleDataAsync(sample, cancellationToken);
        return await RenderPdf(template, data, $"{sample.Name}.pdf", download, cancellationToken);
    }

    /// <summary>
    /// Renders the invoice sample from a strongly typed C# <see cref="Invoice"/> instead of JSON,
    /// showing values bound straight from .NET code.
    /// </summary>
    [HttpGet("/api/examples/invoice")]
    [Produces("application/pdf")]
    public async Task<IActionResult> TypedInvoice(bool paid = true, bool download = false, CancellationToken cancellationToken = default)
    {
        if (store.Find("invoice") is not { } sample)
        {
            return NotFound();
        }

        var template = await store.LoadTemplateAsync(sample, cancellationToken);
        var invoice = Invoice.CreateSample(paid);
        return await RenderPdf(template, invoice, $"{invoice.Number}.pdf", download, cancellationToken);
    }

    private async Task<IActionResult> RenderPdf(
        PdfTemplate template, object model, string fileName, bool download, CancellationToken cancellationToken)
    {
        var pdf = await generator.GenerateAsync(template, model, cancellationToken: cancellationToken);
        return download ? File(pdf, "application/pdf", fileName) : File(pdf, "application/pdf");
    }
}
