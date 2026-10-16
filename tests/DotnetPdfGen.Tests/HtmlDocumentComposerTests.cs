namespace DotnetPdfGen.Tests;

public class HtmlDocumentComposerTests
{
    [Fact]
    public void Injects_css_before_closing_head()
    {
        var result = HtmlDocumentComposer.Compose("<html><head><title>T</title></head><body>x</body></html>", "h1 { color: red; }");

        Assert.Matches(@"<title>T</title><style>\s*h1 \{ color: red; \}\s*</style>\s*</head>", result);
    }

    [Fact]
    public void Adds_head_when_document_has_none()
    {
        var result = HtmlDocumentComposer.Compose("<html lang=\"en\"><body>x</body></html>", "p {}");

        Assert.StartsWith("<html lang=\"en\"><head>\n<style>", result);
        Assert.EndsWith("</head>\n<body>x</body></html>", result);
    }

    [Fact]
    public void Wraps_fragments_in_full_document()
    {
        var result = HtmlDocumentComposer.Compose("<p>Hello</p>", "p {}");

        Assert.StartsWith("<!DOCTYPE html>", result);
        Assert.Contains("<style>", result);
        Assert.Contains("<body>\n<p>Hello</p>\n</body>", result);
    }

    [Fact]
    public void Wraps_fragments_without_css()
    {
        var result = HtmlDocumentComposer.Compose("<p>Hello</p>", null);

        Assert.StartsWith("<!DOCTYPE html>", result);
        Assert.DoesNotContain("<style>", result);
    }

    [Fact]
    public void Leaves_full_document_unchanged_without_css()
    {
        const string html = "<html><head></head><body>x</body></html>";

        Assert.Equal(html, HtmlDocumentComposer.Compose(html, "  "));
    }
}
