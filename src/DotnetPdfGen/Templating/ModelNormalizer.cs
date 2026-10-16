using System.Text.Json;

namespace DotnetPdfGen.Templating;

/// <summary>
/// Converts model types the template engine can't traverse (JSON DOM types) into
/// dictionaries and lists it can. Other models pass through unchanged.
/// </summary>
internal static class ModelNormalizer
{
    public static object? Normalize(object? model) => model switch
    {
        JsonDocument document => FromJson(document.RootElement),
        JsonElement element => FromJson(element),
        _ => model,
    };

    private static object? FromJson(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => FromJsonObject(element),
        JsonValueKind.Array => element.EnumerateArray().Select(FromJson).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetDecimal(out var number) ? number : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };

    // JSON clients commonly camelCase their keys, so {{Title}} also matches "title".
    // If keys differ only by case, the last one wins.
    private static Dictionary<string, object?> FromJsonObject(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = FromJson(property.Value);
        }

        return result;
    }
}
