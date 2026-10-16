using System.Globalization;
using System.Text.RegularExpressions;
using HandlebarsDotNet;
using HandlebarsDotNet.Compiler;

namespace DotnetPdfGen.Templating;

/// <summary>
/// An <see cref="ITemplateBinder"/> that uses Handlebars syntax.
/// </summary>
/// <remarks>
/// Supported syntax includes:
/// <list type="bullet">
///   <item><c>{{Title}}</c> and <c>{{Customer.Name}}</c>: HTML-escaped values.</item>
///   <item><c>{{{RawHtml}}}</c>: unescaped values.</item>
///   <item><c>{{#each Items}}...{{/each}}</c> and <c>{{#if Paid}}...{{else}}...{{/if}}</c>: loops and conditionals.</item>
///   <item><c>{{format Total "C2"}}</c>: formats numbers and dates with a .NET format string.</item>
/// </list>
/// Instances are thread-safe once constructed.
/// </remarks>
public sealed partial class HandlebarsTemplateBinder : ITemplateBinder
{
    private readonly IHandlebars _handlebars;
    private readonly CultureInfo? _culture;

    /// <summary>Creates a binder.</summary>
    public HandlebarsTemplateBinder(TemplateBinderOptions? options = null)
    {
        options ??= new TemplateBinderOptions();
        _culture = options.Culture;
        _handlebars = Handlebars.Create(new HandlebarsConfiguration
        {
            ThrowOnUnresolvedBindingExpression = options.StrictMode,
        });
        _handlebars.RegisterHelper("format", Format);
    }

    /// <inheritdoc />
    public string Bind(string template, object? model)
    {
        ArgumentNullException.ThrowIfNull(template);

        try
        {
            var compiled = _handlebars.Compile(template);
            return compiled(ModelNormalizer.Normalize(model));
        }
        catch (Exception ex) when (ex is HandlebarsException or HandlebarsUndefinedBindingException)
        {
            throw new TemplateBindingException($"Failed to bind template: {ex.Message}", ex);
        }
    }

    private object? Format(Context context, Arguments arguments)
    {
        if (arguments.Length is 0 or > 2)
        {
            throw new HandlebarsException("{{format}} expects a value and an optional format string, e.g. {{format Total \"C2\"}}.");
        }

        var value = arguments[0];
        var format = arguments.Length > 1 ? arguments[1]?.ToString() : null;
        var culture = _culture ?? CultureInfo.CurrentCulture;

        // JSON has no date type, so ISO 8601 date strings are formatted as dates.
        if (value is string text && format is not null && IsoDatePattern().IsMatch(text)
            && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
        {
            value = date;
        }

        return value switch
        {
            null or UndefinedBindingResult => string.Empty,
            IFormattable formattable => formattable.ToString(format, culture),
            _ => value.ToString(),
        };
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}")]
    private static partial Regex IsoDatePattern();
}
