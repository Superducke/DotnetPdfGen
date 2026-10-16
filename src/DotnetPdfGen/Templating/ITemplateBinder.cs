namespace DotnetPdfGen.Templating;

/// <summary>Replaces binding expressions such as <c>{{Title}}</c> in a template with model values.</summary>
public interface ITemplateBinder
{
    /// <summary>Binds <paramref name="model"/> into <paramref name="template"/>.</summary>
    /// <param name="template">The template text.</param>
    /// <param name="model">
    /// The data to bind: a POCO, anonymous object, <see cref="IDictionary{TKey,TValue}"/>,
    /// <see cref="System.Text.Json.JsonElement"/>, <see cref="System.Text.Json.JsonDocument"/>, or <see langword="null"/>.
    /// </param>
    /// <returns>The bound text.</returns>
    /// <exception cref="TemplateBindingException">The template is invalid, or strict mode found an unresolved value.</exception>
    string Bind(string template, object? model);
}

/// <summary>Thrown when a template cannot be parsed or bound.</summary>
public sealed class TemplateBindingException : Exception
{
    /// <summary>Creates the exception.</summary>
    public TemplateBindingException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
