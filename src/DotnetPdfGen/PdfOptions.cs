namespace DotnetPdfGen;

/// <summary>Page setup used when rendering a PDF.</summary>
public sealed record PdfOptions
{
    /// <summary>Default options: A4 portrait with 20mm margins.</summary>
    public static PdfOptions Default { get; } = new();

    /// <summary>The paper size. Defaults to <see cref="PageSize.A4"/>.</summary>
    public PageSize PageSize { get; init; } = PageSize.A4;

    /// <summary>The page orientation. Defaults to <see cref="PageOrientation.Portrait"/>.</summary>
    public PageOrientation Orientation { get; init; } = PageOrientation.Portrait;

    /// <summary>The page margins. Defaults to 20mm on every side.</summary>
    public PageMargins Margins { get; init; } = PageMargins.FromMillimeters(20);

    /// <summary>Optional document title written to the PDF metadata.</summary>
    public string? Title { get; init; }

    /// <summary>Optional author written to the PDF metadata.</summary>
    public string? Author { get; init; }

    /// <summary>The page width and height after applying <see cref="Orientation"/>, in points.</summary>
    public (double Width, double Height) GetPageDimensions()
    {
        var (shortSide, longSide) = PageSize.Width <= PageSize.Height
            ? (PageSize.Width, PageSize.Height)
            : (PageSize.Height, PageSize.Width);

        return Orientation == PageOrientation.Portrait
            ? (shortSide, longSide)
            : (longSide, shortSide);
    }
}

/// <summary>Page orientation.</summary>
public enum PageOrientation
{
    /// <summary>Taller than wide.</summary>
    Portrait,

    /// <summary>Wider than tall.</summary>
    Landscape,
}

/// <summary>A paper size in PDF points (1/72 inch), given in portrait orientation.</summary>
/// <param name="Width">The width in points.</param>
/// <param name="Height">The height in points.</param>
public readonly record struct PageSize(double Width, double Height)
{
    /// <summary>ISO A3 (297 × 420 mm).</summary>
    public static PageSize A3 { get; } = FromMillimeters(297, 420);

    /// <summary>ISO A4 (210 × 297 mm).</summary>
    public static PageSize A4 { get; } = FromMillimeters(210, 297);

    /// <summary>ISO A5 (148 × 210 mm).</summary>
    public static PageSize A5 { get; } = FromMillimeters(148, 210);

    /// <summary>US Letter (8.5 × 11 in).</summary>
    public static PageSize Letter { get; } = FromInches(8.5, 11);

    /// <summary>US Legal (8.5 × 14 in).</summary>
    public static PageSize Legal { get; } = FromInches(8.5, 14);

    /// <summary>Creates a page size from millimeters.</summary>
    public static PageSize FromMillimeters(double width, double height) =>
        new(Units.MillimetersToPoints(width), Units.MillimetersToPoints(height));

    /// <summary>Creates a page size from inches.</summary>
    public static PageSize FromInches(double width, double height) =>
        new(Units.InchesToPoints(width), Units.InchesToPoints(height));
}

/// <summary>Page margins in PDF points (1/72 inch).</summary>
public readonly record struct PageMargins(double Top, double Right, double Bottom, double Left)
{
    /// <summary>No margins.</summary>
    public static PageMargins None { get; } = new(0, 0, 0, 0);

    /// <summary>Creates equal margins on every side, in points.</summary>
    public static PageMargins Uniform(double points) => new(points, points, points, points);

    /// <summary>Creates equal margins on every side, in millimeters.</summary>
    public static PageMargins FromMillimeters(double all) => Uniform(Units.MillimetersToPoints(all));

    /// <summary>Creates margins from millimeters.</summary>
    public static PageMargins FromMillimeters(double top, double right, double bottom, double left) => new(
        Units.MillimetersToPoints(top),
        Units.MillimetersToPoints(right),
        Units.MillimetersToPoints(bottom),
        Units.MillimetersToPoints(left));
}

internal static class Units
{
    private const double PointsPerInch = 72.0;
    private const double MillimetersPerInch = 25.4;

    public static double InchesToPoints(double inches) => inches * PointsPerInch;

    public static double MillimetersToPoints(double millimeters) => millimeters / MillimetersPerInch * PointsPerInch;
}
