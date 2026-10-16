using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DotnetPdfGen.Web.Contracts;

/// <summary>An ad-hoc template to render, as sent by the preview page.</summary>
public sealed record RenderRequest
{
    /// <summary>The HTML template with <c>{{...}}</c> binding expressions.</summary>
    [Required]
    public string Html { get; init; } = string.Empty;

    /// <summary>Optional CSS injected into the document head.</summary>
    public string? Css { get; init; }

    /// <summary>The model to bind, as any JSON value.</summary>
    public JsonElement? Data { get; init; }

    /// <summary>Page setup. CSS <c>@page</c> rules in the template take precedence.</summary>
    public PageSetup? Page { get; init; }
}

/// <summary>Page setup for a render request.</summary>
public sealed record PageSetup
{
    private static readonly Dictionary<string, PageSize> Sizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A3"] = PageSize.A3,
        ["A4"] = PageSize.A4,
        ["A5"] = PageSize.A5,
        ["Letter"] = PageSize.Letter,
        ["Legal"] = PageSize.Legal,
    };

    public static IEnumerable<string> SupportedSizes => Sizes.Keys;

    /// <summary>A3, A4, A5, Letter or Legal.</summary>
    public string Size { get; init; } = "A4";

    public PageOrientation Orientation { get; init; } = PageOrientation.Portrait;

    /// <summary>Margin on every side, in millimeters.</summary>
    [Range(0, 100)]
    public double MarginMm { get; init; } = 20;

    public bool TryToOptions(out PdfOptions options)
    {
        options = PdfOptions.Default;
        if (!Sizes.TryGetValue(Size, out var size))
        {
            return false;
        }

        options = new PdfOptions { PageSize = size, Orientation = Orientation, Margins = PageMargins.FromMillimeters(MarginMm) };
        return true;
    }
}
