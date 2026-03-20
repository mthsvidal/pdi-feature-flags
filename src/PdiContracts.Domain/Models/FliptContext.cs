using System.Text.Json;

namespace PdiContracts.Domain.Models;

/// <summary>
/// Contexto enviado ao Flipt para avaliacao de feature flags.
/// </summary>
public class FliptContext
{
    /// <summary>
    /// Identificador estavel da entidade para split e rollout.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Atributos de contexto no formato campo/valor.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();

    /// <summary>
    /// Adiciona um objeto JSON como atributos do contexto, achatando multiplos niveis.
    /// </summary>
    public void AddJsonAttribute(string key, string jsonValue)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonValue);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object || root.ValueKind == JsonValueKind.Array)
            {
                var flattened = FlattenJsonElement(key, root);
                foreach (var item in flattened)
                {
                    Attributes[item.Key] = item.Value;
                }
            }
            else
            {
                Attributes[key] = jsonValue;
            }
        }
        catch (JsonException)
        {
            Attributes[key] = jsonValue;
        }
    }

    private static Dictionary<string, string> FlattenJsonElement(string prefix, JsonElement element)
    {
        var result = new Dictionary<string, string>();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    var flattened = FlattenJsonElement(key, property.Value);
                    foreach (var item in flattened)
                    {
                        result[item.Key] = item.Value;
                    }
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}[{index}]";
                    var flattened = FlattenJsonElement(key, item);
                    foreach (var flatItem in flattened)
                    {
                        result[flatItem.Key] = flatItem.Value;
                    }
                    index++;
                }
                break;

            case JsonValueKind.String:
                result[prefix] = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
                result[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                result[prefix] = element.GetBoolean().ToString().ToLowerInvariant();
                break;

            case JsonValueKind.Null:
                result[prefix] = string.Empty;
                break;
        }

        return result;
    }
}