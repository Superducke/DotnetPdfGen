namespace DotnetPdfGen.Tests;

public class PdfOptionsTests
{
    [Fact]
    public void A4_is_595_by_842_points()
    {
        Assert.Equal(595.28, PageSize.A4.Width, precision: 2);
        Assert.Equal(841.89, PageSize.A4.Height, precision: 2);
    }

    [Fact]
    public void Letter_is_612_by_792_points()
    {
        Assert.Equal(new PageSize(612, 792), PageSize.Letter);
    }

    [Fact]
    public void Landscape_swaps_dimensions()
    {
        var options = new PdfOptions { PageSize = PageSize.Letter, Orientation = PageOrientation.Landscape };

        Assert.Equal((792d, 612d), options.GetPageDimensions());
    }

    [Fact]
    public void Margins_convert_from_millimeters()
    {
        Assert.Equal(72, PageMargins.FromMillimeters(25.4).Top, precision: 6);
    }
}
