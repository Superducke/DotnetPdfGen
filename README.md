# DotnetPdfGen

Generate PDF documents in .NET from **HTML + CSS templates** with **`{{Value}}` data binding**.

```csharp
var template = new PdfTemplate("<h1>Invoice {{Number}}</h1><p>Bill to {{Customer.Name}}</p>");
byte[] pdf = await generator.GenerateAsync(template, new { Number = "INV-001", Customer = new { Name = "Ada" } });
```

- **Pure .NET.** No headless browser, no external process, no native binaries to ship. It runs anywhere .NET 8+ runs, including Linux containers.
- **Modern CSS.** Flexbox, `@page` rules with page numbers and running headers, page breaks, `@font-face`, gradients, transforms and SVG, rendered by [PeachPDF](https://peachpdf.net/).
- **Handlebars templates.** Values are HTML-escaped by default. Templates support loops, conditionals and a `{{format}}` helper for numbers and dates.
- **Any model.** POCOs, records, anonymous objects, dictionaries, or raw JSON (`JsonElement`/`JsonDocument`).
- **DI-ready.** One call to `services.AddDotnetPdfGen()`, and the binder and renderer can be swapped through interfaces.

## Contents

- [Install](#install)
- [Quick start](#quick-start)
- [ASP.NET Core](#aspnet-core)
- [Template syntax](#template-syntax)
- [Models](#models)
- [Templates on disk and relative assets](#templates-on-disk-and-relative-assets)
- [Page setup](#page-setup)
- [Fonts](#fonts)
- [Security](#security)
- [CSS support](#css-support)
- [Performance](#performance)
- [Extending](#extending)
- [Demo app](#demo-app)
- [Building from source](#building-from-source)

## Install

```bash
dotnet add package DotnetPdfGen
```

Targets `net8.0` and `net10.0`.

## Quick start

```csharp
using DotnetPdfGen;
using DotnetPdfGen.Rendering;
using DotnetPdfGen.Templating;

var generator = new PdfGenerator(new HandlebarsTemplateBinder(), new PeachPdfRenderer());

var template = new PdfTemplate(
    html: """
        <h1>{{Title}}</h1>
        <p>Prepared for {{Customer.Name}} on {{format Date "d MMMM yyyy"}}.</p>
        <table>
          {{#each Items}}
          <tr><td>{{Name}}</td><td class="num">{{format Price "C"}}</td></tr>
          {{/each}}
        </table>
        """,
    css: """
        body { font-family: Arial, sans-serif; }
        h1 { color: #1f4e79; }
        .num { text-align: right; }
        """);

var model = new
{
    Title = "Quote",
    Date = new DateTime(2026, 10, 16),
    Customer = new { Name = "Ada Lovelace" },
    Items = new[] { new { Name = "Consulting", Price = 1200m }, new { Name = "Support", Price = 300m } },
};

byte[] pdf = await generator.GenerateAsync(template, model);
await File.WriteAllBytesAsync("quote.pdf", pdf);
```

`PdfGenerator`, `HandlebarsTemplateBinder` and `PeachPdfRenderer` are thread-safe, so create them once and reuse them.

## ASP.NET Core

Register the services once:

```csharp
builder.Services.AddDotnetPdfGen(options =>
{
    options.Binding.StrictMode = true;                        // throw on {{Missing}} values
    options.Binding.Culture = CultureInfo.GetCultureInfo("en-GB");
    options.Rendering.FontFamilyMappings["Segoe UI"] = "Arial";
});
```

Then inject `IPdfGenerator`:

```csharp
app.MapGet("/invoices/{id}.pdf", async (int id, IPdfGenerator pdfs, InvoiceStore store, CancellationToken ct) =>
{
    var invoice = await store.GetAsync(id, ct);
    var template = await PdfTemplate.FromFilesAsync("Templates/invoice.html", "Templates/invoice.css", ct);

    var pdf = await pdfs.GenerateAsync(template, invoice, cancellationToken: ct);
    return Results.File(pdf, "application/pdf", $"invoice-{id}.pdf");
});
```

To write straight to a stream instead of a `byte[]`, use `GenerateToStreamAsync`. It only writes asynchronously, so it is safe to pass `HttpResponse.Body`.

## Template syntax

Templates use [Handlebars](https://handlebarsjs.com/guide/) syntax via [Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net).

| Syntax | Meaning |
|---|---|
| `{{Title}}` | Insert a value, HTML-escaped |
| `{{Customer.Address.City}}` | Nested properties |
| `{{{RawHtml}}}` | Insert a value **without** escaping. Only use this for trusted content |
| `{{#each Items}}…{{/each}}` | Loop. Inside, `{{this}}` is the item and `{{@index}}` is its zero-based position |
| `{{#if Paid}}…{{else}}…{{/if}}` | Conditional. `false`, `null`, `0`, `""` and empty lists are falsy |
| `{{#unless Paid}}…{{/unless}}` | Inverse conditional |
| `{{format Total "N2"}}` | Format with any .NET format string: `"C"`, `"N0"`, `"P1"`, `"yyyy-MM-dd"`, … |

`{{format}}` uses `TemplateBinderOptions.Culture`, or the current culture when that isn't set. It works on any `IFormattable`, such as numbers, `DateTime`, `DateOnly` and `DateTimeOffset`. It also formats ISO 8601 date strings like `"2026-10-16"` as dates, because JSON has no date type.

By default a missing value renders as an empty string. With `StrictMode = true` it throws a `TemplateBindingException` instead. A malformed template, such as an unclosed `{{#each}}`, always throws `TemplateBindingException`.

**Keep logic in C#.** Templates can't do arithmetic, so put computed values on the model:

```csharp
public sealed record InvoiceLine(string Description, int Quantity, decimal UnitPrice)
{
    public decimal Amount => Quantity * UnitPrice;   // {{format Amount "N2"}}
}
```

To preview the final HTML for debugging, call `generator.ComposeHtml(template, model)`. It returns the bound HTML document exactly as it is sent to the renderer.

## Models

| Model | Notes |
|---|---|
| Classes and records | Public properties are bound by name. Computed properties work too |
| Anonymous objects | Handy for one-off documents |
| `IDictionary<string, object?>` | Keys are bound by name |
| `JsonElement` / `JsonDocument` | Objects, arrays, strings, numbers and booleans. Keys match **case-insensitively**, so camelCase JSON binds to `{{Title}}` |
| `null` | Allowed. Every binding renders empty, or throws in strict mode |

## Templates on disk and relative assets

```csharp
var template = await PdfTemplate.FromFilesAsync("Templates/report/template.html", "Templates/report/styles.css");
```

`FromFilesAsync` sets `PdfTemplate.BasePath` to the HTML file's folder. Relative URLs such as `<img src="logo.png">`, `url(fonts/brand.woff2)` and `<link href="extra.css">` then resolve next to the template. You can also set `BasePath` yourself:

```csharp
var template = new PdfTemplate(html, css) { BasePath = "/app/templates/invoice" };
```

`data:` URIs always work. They're the simplest option for small logos when no base path is set.

## Page setup

```csharp
var options = new PdfOptions
{
    PageSize = PageSize.Letter,                       // A3, A4 (default), A5, Letter, Legal, or custom
    Orientation = PageOrientation.Landscape,
    Margins = PageMargins.FromMillimeters(15),        // default: 20mm on every side
    Title = "Quarterly report",                       // PDF metadata
    Author = "Finance",
};

byte[] pdf = await generator.GenerateAsync(template, model, options);
```

Custom sizes are in points (1/72 inch), or use `PageSize.FromMillimeters(…)` / `PageSize.FromInches(…)`.

**CSS `@page` rules take precedence over `PdfOptions`**, so a template can own its page setup. Page numbers, running headers and page breaks are all plain CSS:

```css
@page {
  size: A4 landscape;
  margin: 20mm 15mm;
  @bottom-center { content: "Page " counter(page) " of " counter(pages); font-size: 8pt; }
  @top-left { content: element(doc-title); }
}

.doc-title  { position: running(doc-title); }   /* repeated in every page's header */
.appendix   { page-break-before: always; }
.keep       { page-break-inside: avoid; }
```

Table headers (`<thead>`) repeat automatically when a table spans pages.

## Fonts

Fonts installed on the operating system are found automatically. Generic families such as `sans-serif` map to real installed fonts per platform. Templates can also load fonts with `@font-face`.

To ship fonts with your app, or to substitute fonts the server doesn't have:

```csharp
builder.Services.AddDotnetPdfGen(options =>
{
    options.Rendering.FontFiles.Add("Fonts/Inter-Regular.ttf");   // TTF, OTF, WOFF, WOFF2
    options.Rendering.FontFiles.Add("Fonts/Inter-Bold.ttf");
    options.Rendering.FontFamilyMappings["Segoe UI"] = "Inter";
});
```

> Minimal Linux container images often have **no fonts at all**. Install a font package such as `fonts-dejavu-core` or `fonts-liberation`, or register font files as shown above.

## Security

- **Values are HTML-escaped.** `{{Value}}` escapes model data, so user-supplied strings can't inject markup. Only `{{{Value}}}` skips escaping.
- **Local file access is opt-in.** A template with no `BasePath` cannot read the local file system: `file:` URLs and relative paths are refused. Setting `BasePath` marks the template as trusted. You can override this either way with `PeachPdfRendererOptions.AllowLocalFileAccess`.
- **No network access.** The renderer doesn't fetch `http(s)` URLs, so templates can't be used to make requests from your server.
- **No JavaScript.** Scripts in templates are not executed.

If you accept templates from users, leave `BasePath` unset, limit request sizes, and consider a timeout. Rendering is CPU-bound and can't be cancelled midway.

## CSS support

Rendering is done by [PeachPDF](https://github.com/jhaygood86/PeachPDF), a pure .NET HTML/CSS layout engine. It covers most of what documents need:

- Flexbox and multi-column layout, tables, floats
- `@page`, margin boxes, `counter(page)` / `counter(pages)`, running elements, page breaks
- Custom properties (`var()`), `calc()`, gradients, transforms, `border-radius`
- `@font-face`, SVG (inline and as images), raster images such as PNG and JPEG, `data:` URIs

It is not a browser engine, so some newer or more obscure features may be missing. See PeachPDF's [HTML & CSS support matrix](https://peachpdf.net/html-css-support.html), and check output in the demo app's live preview while designing templates.

## Performance

The **first render in a process takes about 2 seconds** while fonts are discovered. Later renders of a typical one-to-three-page document take around **100–200 ms**. Warm up at startup to keep that delay off the first request:

```csharp
var app = builder.Build();
await app.Services.GetRequiredService<IPdfGenerator>()
    .GenerateAsync(new PdfTemplate("<p>warm-up</p>"), model: null);
```

Renders run independently, so concurrent requests are safe.

## Extending

Both stages sit behind interfaces:

| Interface | Default | Purpose |
|---|---|---|
| `ITemplateBinder` | `HandlebarsTemplateBinder` | Turns template + model into HTML |
| `IPdfRenderer` | `PeachPdfRenderer` | Turns a complete HTML document into PDF bytes |

Register your own implementation **before** `AddDotnetPdfGen()` and it is used instead of the default:

```csharp
builder.Services.AddSingleton<IPdfRenderer, MyChromiumRenderer>();
builder.Services.AddDotnetPdfGen();
```

## Demo app

`src/DotnetPdfGen.Web` is an ASP.NET Core app that shows the library in use. It has a **live playground** where you edit HTML, CSS and JSON on one side and see the PDF update on the other.

```bash
dotnet run --project src/DotnetPdfGen.Web --launch-profile http
```

Then open http://localhost:5134.

| Endpoint | Description |
|---|---|
| `GET /` | Live preview playground |
| `GET /api/samples` | List the sample templates (invoice, report, certificate) |
| `GET /api/samples/{name}` | A sample's HTML, CSS and sample data |
| `GET /api/samples/{name}/pdf` | Render a sample (`?download=true` to save it) |
| `GET /api/examples/invoice` | The invoice rendered from a **typed C# record** (`?paid=false` to hide the stamp) |
| `POST /api/pdf` | Render `{ html, css, data, page }` to PDF |
| `POST /api/pdf/html` | Return the bound HTML without rendering |

## Building from source

```text
src/DotnetPdfGen/          the library (NuGet package)
src/DotnetPdfGen.Web/      demo app and playground
tests/DotnetPdfGen.Tests/      library unit and rendering tests
tests/DotnetPdfGen.Web.Tests/  demo app API tests
```

```bash
dotnet test DotnetPdfGen.slnx
```

```bash
dotnet pack src/DotnetPdfGen -c Release -o artifacts
```

Requires the .NET 10 SDK.

## License

[MIT](https://github.com/Superducke/DotnetPdfGen/blob/main/LICENSE). Rendering uses PeachPDF (BSD-3-Clause) and templating uses Handlebars.Net (MIT).
