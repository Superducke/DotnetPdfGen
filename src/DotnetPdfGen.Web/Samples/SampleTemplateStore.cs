using System.Text.Json;

namespace DotnetPdfGen.Web.Samples;

/// <summary>A bundled sample template: <c>Templates/{Name}/template.html</c> plus styles, sample data and metadata.</summary>
public sealed record SampleTemplate(string Name, string Title, string Description, string Directory)
{
    public string HtmlPath => Path.Combine(Directory, "template.html");

    public string CssPath => Path.Combine(Directory, "styles.css");

    public string SampleDataPath => Path.Combine(Directory, "sample.json");
}

/// <summary>Discovers the sample templates under <c>{ContentRoot}/Templates</c> at startup.</summary>
public sealed class SampleTemplateStore
{
    private readonly Dictionary<string, SampleTemplate> _templates;

    public SampleTemplateStore(IWebHostEnvironment environment)
    {
        var root = Path.Combine(environment.ContentRootPath, "Templates");
        _templates = Directory.Exists(root)
            ? Directory.EnumerateDirectories(root)
                .Where(dir => File.Exists(Path.Combine(dir, "template.html")))
                .Select(Load)
                .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase)
            : [];
    }

    public IEnumerable<SampleTemplate> All => _templates.Values;

    // Lookups go through the discovered set, so a name can never be used to reach other paths.
    public SampleTemplate? Find(string name) => _templates.GetValueOrDefault(name);

    public Task<PdfTemplate> LoadTemplateAsync(SampleTemplate sample, CancellationToken cancellationToken) =>
        PdfTemplate.FromFilesAsync(sample.HtmlPath, File.Exists(sample.CssPath) ? sample.CssPath : null, cancellationToken);

    public async Task<JsonElement> LoadSampleDataAsync(SampleTemplate sample, CancellationToken cancellationToken)
    {
        if (!File.Exists(sample.SampleDataPath))
        {
            return JsonSerializer.SerializeToElement(new { });
        }

        await using var stream = File.OpenRead(sample.SampleDataPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private static SampleTemplate Load(string directory)
    {
        var name = Path.GetFileName(directory);
        var metadataPath = Path.Combine(directory, "template.json");
        var metadata = File.Exists(metadataPath)
            ? JsonSerializer.Deserialize<Metadata>(File.ReadAllText(metadataPath), JsonSerializerOptions.Web)
            : null;

        return new SampleTemplate(name, metadata?.Title ?? name, metadata?.Description ?? string.Empty, directory);
    }

    private sealed record Metadata(string? Title, string? Description);
}
