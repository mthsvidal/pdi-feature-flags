using System.Text.Json;
using PdiContracts.Domain.Models;

namespace PdiContracts.Domain.Services;

/// <summary>
/// Serviço para gerenciar feature flags, aceitando contexto via JSON
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IFliptClient _fliptClient;

    public FeatureFlagService(IFliptClient fliptClient)
    {
        _fliptClient = fliptClient ?? throw new ArgumentNullException(nameof(fliptClient));
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnabledAsync(string featureName, string? contextJson = null, bool defaultValue = false)
    {
        var context = ParseContext(contextJson);
        return await _fliptClient.IsEnabledAsync(featureName, context, defaultValue);
    }

    /// <summary>
    /// Converte JSON string em contexto para avaliacao no Flipt.
    /// </summary>
    /// <param name="contextJson">JSON com propriedades do contexto</param>
    /// <returns>FliptContext ou null se JSON for vazio/invalido</returns>
    private FliptContext? ParseContext(string? contextJson)
    {
        if (string.IsNullOrWhiteSpace(contextJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(contextJson);
            var root = document.RootElement;

            var context = new FliptContext
            {
                Attributes = new Dictionary<string, string>()
            };

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("entityId", out var entityIdElement) &&
                entityIdElement.ValueKind == JsonValueKind.String)
            {
                context.EntityId = entityIdElement.GetString();
            }

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("userId", out var userIdElement) &&
                userIdElement.ValueKind == JsonValueKind.String &&
                string.IsNullOrWhiteSpace(context.EntityId))
            {
                context.EntityId = userIdElement.GetString();
            }

            // Converte todo o JSON (incluindo nested objects) em dicionario achatado.
            FlattenJsonElement(root, "", context.Attributes);

            return context;
        }
        catch (JsonException)
        {
            // Se JSON for inválido, retorna contexto vazio ao invés de lançar exceção
            return null;
        }
    }

    /// <summary>
    /// Achata um JsonElement recursivamente em um dicionário com notação de ponto para objetos nested
    /// </summary>
    /// <param name="element">Elemento JSON a ser achatado</param>
    /// <param name="prefix">Prefixo para a chave (usado na recursão)</param>
    /// <param name="properties">Dicionário onde as propriedades achatadas serão adicionadas</param>
    private void FlattenJsonElement(JsonElement element, string prefix, Dictionary<string, string> properties)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJsonElement(property.Value, key, properties);
                }
                break;

            case JsonValueKind.Array:
                var arrayIndex = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}[{arrayIndex}]";
                    FlattenJsonElement(item, key, properties);
                    arrayIndex++;
                }
                break;

            case JsonValueKind.String:
                if (!string.IsNullOrEmpty(prefix))
                {
                    properties[prefix] = element.GetString() ?? "";
                }
                break;

            case JsonValueKind.Number:
                if (!string.IsNullOrEmpty(prefix))
                {
                    properties[prefix] = element.GetRawText();
                }
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                if (!string.IsNullOrEmpty(prefix))
                {
                    properties[prefix] = element.GetBoolean().ToString().ToLowerInvariant();
                }
                break;

            case JsonValueKind.Null:
                if (!string.IsNullOrEmpty(prefix))
                {
                    properties[prefix] = "null";
                }
                break;
        }
    }
}
