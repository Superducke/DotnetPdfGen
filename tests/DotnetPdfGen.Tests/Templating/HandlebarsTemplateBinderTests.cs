using System.Globalization;
using System.Text.Json;
using DotnetPdfGen.Templating;

namespace DotnetPdfGen.Tests.Templating;

public class HandlebarsTemplateBinderTests
{
    private readonly HandlebarsTemplateBinder _binder = new(new TemplateBinderOptions { Culture = CultureInfo.InvariantCulture });

    [Fact]
    public void Binds_simple_property_from_anonymous_object()
    {
        var result = _binder.Bind("<h1>{{Title}}</h1>", new { Title = "Invoice" });

        Assert.Equal("<h1>Invoice</h1>", result);
    }

    [Fact]
    public void Binds_nested_properties_from_poco()
    {
        var model = new Order("A-100", new Customer("Ada Lovelace"), []);

        var result = _binder.Bind("{{Number}} for {{Customer.Name}}", model);

        Assert.Equal("A-100 for Ada Lovelace", result);
    }

    [Fact]
    public void Binds_from_dictionary()
    {
        var model = new Dictionary<string, object?> { ["Title"] = "Report" };

        Assert.Equal("Report", _binder.Bind("{{Title}}", model));
    }

    [Fact]
    public void Html_escapes_double_brace_values()
    {
        var result = _binder.Bind("{{Value}}", new { Value = "<script>alert('x')</script>" });

        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void Triple_braces_emit_raw_html()
    {
        var result = _binder.Bind("{{{Value}}}", new { Value = "<b>bold</b>" });

        Assert.Equal("<b>bold</b>", result);
    }

    [Fact]
    public void Each_iterates_collections()
    {
        var model = new Order("A-1", new Customer("x"), [new LineItem("Pen", 2), new LineItem("Ink", 1)]);

        var result = _binder.Bind("{{#each Items}}<tr><td>{{Name}}</td><td>{{Quantity}}</td></tr>{{/each}}", model);

        Assert.Equal("<tr><td>Pen</td><td>2</td></tr><tr><td>Ink</td><td>1</td></tr>", result);
    }

    [Theory]
    [InlineData(true, "PAID")]
    [InlineData(false, "DUE")]
    public void If_else_renders_branch(bool paid, string expected)
    {
        var result = _binder.Bind("{{#if Paid}}PAID{{else}}DUE{{/if}}", new { Paid = paid });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Missing_values_render_empty_by_default()
    {
        var result = _binder.Bind("[{{Missing}}]", new { Title = "x" });

        Assert.Equal("[]", result);
    }

    [Fact]
    public void Missing_values_throw_in_strict_mode()
    {
        var strict = new HandlebarsTemplateBinder(new TemplateBinderOptions { StrictMode = true });

        Assert.Throws<TemplateBindingException>(() => strict.Bind("{{Missing}}", new { Title = "x" }));
    }

    [Fact]
    public void Null_model_is_allowed()
    {
        Assert.Equal("Static []", _binder.Bind("Static [{{Title}}]", null));
    }

    [Fact]
    public void Invalid_template_throws_binding_exception()
    {
        Assert.Throws<TemplateBindingException>(() => _binder.Bind("{{#each Items}}unclosed", new { Items = new[] { 1 } }));
    }

    [Fact]
    public void Format_helper_formats_numbers_and_dates()
    {
        var model = new { Total = 1234.5m, Date = new DateTime(2026, 10, 16) };

        var result = _binder.Bind("{{format Total \"N2\"}} on {{format Date \"yyyy-MM-dd\"}}", model);

        Assert.Equal("1,234.50 on 2026-10-16", result);
    }

    [Fact]
    public void Format_helper_uses_configured_culture()
    {
        var german = new HandlebarsTemplateBinder(new TemplateBinderOptions { Culture = CultureInfo.GetCultureInfo("de-DE") });

        Assert.Equal("1.234,50", german.Bind("{{format Total \"N2\"}}", new { Total = 1234.5m }));
    }

    [Fact]
    public void Format_helper_renders_missing_value_as_empty()
    {
        Assert.Equal("[]", _binder.Bind("[{{format Missing \"N2\"}}]", new { }));
    }

    [Fact]
    public void Binds_json_element_models()
    {
        using var json = JsonDocument.Parse("""
            {
              "Title": "From JSON",
              "Total": 12.5,
              "Issued": "2026-10-16",
              "Items": [ { "Name": "A" }, { "Name": "B" } ],
              "Paid": false
            }
            """);

        var template = "{{Title}}|{{format Total \"N2\"}}|{{format Issued \"dd MMM yyyy\"}}|{{#each Items}}{{Name}}{{/each}}|{{#if Paid}}yes{{else}}no{{/if}}";

        Assert.Equal("From JSON|12.50|16 Oct 2026|AB|no", _binder.Bind(template, json));
        Assert.Equal("From JSON|12.50|16 Oct 2026|AB|no", _binder.Bind(template, json.RootElement));
    }

    [Fact]
    public void Json_keys_match_case_insensitively()
    {
        using var json = JsonDocument.Parse("""{ "title": "camel", "customer": { "name": "Ada" } }""");

        Assert.Equal("camel / Ada", _binder.Bind("{{Title}} / {{Customer.Name}}", json));
    }

    private sealed record Order(string Number, Customer Customer, IReadOnlyList<LineItem> Items);

    private sealed record Customer(string Name);

    private sealed record LineItem(string Name, int Quantity);
}
