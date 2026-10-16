using System.Diagnostics;
using DotnetPdfGen.Web.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace DotnetPdfGen.Web.Controllers;

/// <summary>Renders ad-hoc templates posted by a client, such as the live preview page.</summary>
/// <remarks>
/// Posted HTML is untrusted, so these templates get no base path: they cannot read local files.
/// </remarks>
[ApiController]
[Route("api/pdf")]
[RequestSizeLimit(MaxRequestBytes)]
public sealed class PdfController(IPdfGenerator generator) : ControllerBase
{
    private const int MaxRequestBytes = 2 * 1024 * 1024;

    /// <summary>Binds the data into the template and returns the rendered PDF.</summary>
    [HttpPost]
    [Produces("application/pdf")]
    public async Task<IActionResult> Render(RenderRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetOptions(request, out var options))
        {
            return ValidationProblem(ModelState);
        }

        var stopwatch = Stopwatch.StartNew();
        var pdf = await generator.GenerateAsync(ToTemplate(request), request.Data, options, cancellationToken);
        Response.Headers["X-Render-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
        return File(pdf, "application/pdf");
    }

    /// <summary>Returns the bound HTML document without rendering it, for debugging templates.</summary>
    [HttpPost("html")]
    [Produces("text/html")]
    public ContentResult ComposeHtml(RenderRequest request) =>
        Content(generator.ComposeHtml(ToTemplate(request), request.Data), "text/html; charset=utf-8");

    private static PdfTemplate ToTemplate(RenderRequest request) => new(request.Html, request.Css);

    private bool TryGetOptions(RenderRequest request, out PdfOptions options)
    {
        if ((request.Page ?? new PageSetup()).TryToOptions(out options))
        {
            return true;
        }

        ModelState.AddModelError("page.size", $"Unknown page size. Supported: {string.Join(", ", PageSetup.SupportedSizes)}.");
        return false;
    }
}
