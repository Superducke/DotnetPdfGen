using System.Globalization;

namespace DotnetPdfGen.Templating;

/// <summary>Options for <see cref="HandlebarsTemplateBinder"/>.</summary>
public sealed class TemplateBinderOptions
{
    /// <summary>
    /// When <see langword="true"/>, a binding expression that does not resolve to a model value
    /// throws <see cref="TemplateBindingException"/>. When <see langword="false"/> (the default),
    /// it renders as an empty string.
    /// </summary>
    public bool StrictMode { get; set; }

    /// <summary>
    /// The culture used by the <c>{{format}}</c> helper. When <see langword="null"/> (the default),
    /// <see cref="CultureInfo.CurrentCulture"/> at bind time is used.
    /// </summary>
    public CultureInfo? Culture { get; set; }
}
