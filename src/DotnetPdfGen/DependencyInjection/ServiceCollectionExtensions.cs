using DotnetPdfGen;
using DotnetPdfGen.Rendering;
using DotnetPdfGen.Templating;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Lives in the Microsoft namespace so AddDotnetPdfGen is discoverable without an extra using.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers DotnetPdfGen services.</summary>
public static class DotnetPdfGenServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IPdfGenerator"/> with the Handlebars template binder and the PeachPDF renderer.
    /// </summary>
    /// <remarks>
    /// Existing <see cref="ITemplateBinder"/> or <see cref="IPdfRenderer"/> registrations are kept,
    /// so either can be replaced by registering it before calling this method.
    /// </remarks>
    public static IServiceCollection AddDotnetPdfGen(this IServiceCollection services, Action<DotnetPdfGenOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DotnetPdfGenOptions();
        configure?.Invoke(options);

        services.TryAddSingleton<ITemplateBinder>(_ => new HandlebarsTemplateBinder(options.Binding));
        services.TryAddSingleton<IPdfRenderer>(_ => new PeachPdfRenderer(options.Rendering));
        services.TryAddSingleton<IPdfGenerator, PdfGenerator>();
        return services;
    }
}

/// <summary>Options passed to <see cref="DotnetPdfGenServiceCollectionExtensions.AddDotnetPdfGen"/>.</summary>
public sealed class DotnetPdfGenOptions
{
    /// <summary>Template binding options.</summary>
    public TemplateBinderOptions Binding { get; } = new();

    /// <summary>PDF rendering options.</summary>
    public PeachPdfRendererOptions Rendering { get; } = new();
}
