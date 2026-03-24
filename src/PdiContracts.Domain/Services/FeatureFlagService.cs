using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PdiContracts.Domain.Models;

namespace PdiContracts.Domain.Services;

/// <summary>
/// Serviço para gerenciar feature flags, aceitando contexto via JSON
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IFliptClient _fliptClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(
        IFliptClient fliptClient,
        IDistributedCache cache,
        ILogger<FeatureFlagService> logger)
    {
        _fliptClient = fliptClient ?? throw new ArgumentNullException(nameof(fliptClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnabledAsync(string featureName, string? contextJson = null, bool defaultValue = false)
    {
        var cacheKey = BuildCacheKey(featureName, contextJson, defaultValue);

        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey);
            if (bool.TryParse(cachedValue, out var parsedCachedValue))
            {
                return parsedCachedValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar cache Redis para a feature {FeatureName}", featureName);
        }

        var context = ParseContext(contextJson);
        var isEnabled = await _fliptClient.IsEnabledAsync(featureName, context, defaultValue);

        try
        {
            await _cache.SetStringAsync(
                cacheKey,
                isEnabled.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar cache Redis para a feature {FeatureName}", featureName);
        }

        return isEnabled;
    }

    private static string BuildCacheKey(string featureName, string? contextJson, bool defaultValue)
    {
        // Se houver contextJson, extrai todos os campos EXCETO entityId
        var cacheKeyPart = "default";
        
        if (!string.IsNullOrWhiteSpace(contextJson))
        {
            try
            {
                using var document = JsonDocument.Parse(contextJson);
                var root = document.RootElement;
                
                var contextValues = new List<string>();
                
                foreach (var property in root.EnumerateObject())
                {
                    // Pula entityId - não faz parte da chave
                    if (property.Name.Equals("entityId", StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    var value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? "",
                        JsonValueKind.Number => property.Value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => ""
                    };
                    
                    if (!string.IsNullOrEmpty(value))
                    {
                        contextValues.Add($"{property.Name}_{value}");
                    }
                }
                
                if (contextValues.Any())
                {
                    cacheKeyPart = string.Join("-", contextValues);
                }
            }
            catch (JsonException)
            {
                cacheKeyPart = "invalid";
            }
        }
        
        return $"flipt:feature:{featureName}-{cacheKeyPart}";
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
