using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PdiContracts.Domain.Models;

namespace PdiContracts.Domain;

/// <summary>
/// Cliente REST para avaliacao de feature flags no Flipt.
/// </summary>
public class FliptClient : IFliptClient
{
    private readonly HttpClient _httpClient;
    private readonly FliptSettings _settings;
    private readonly ILogger<FliptClient>? _logger;

    public FliptClient(FliptSettings settings, ILogger<FliptClient>? logger = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds))
        };
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnabledAsync(string featureName, FliptContext? context = null, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(featureName))
        {
            throw new ArgumentException("Feature name nao pode ser vazio", nameof(featureName));
        }

        try
        {
            var evaluation = await EvaluateBooleanAsync(featureName, context);
            return evaluation ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao avaliar feature no Flipt: {Feature}", featureName);
            return defaultValue;
        }
    }

    private async Task<bool?> EvaluateBooleanAsync(string featureName, FliptContext? context)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            _logger?.LogWarning("FLIPT_API_TOKEN nao configurado. Nao foi possivel avaliar a feature {Feature}.", featureName);
            return null;
        }

        var endpoint = $"{_settings.FliptUrl.TrimEnd('/')}/evaluate/v1/batch";

        var requestBody = new
        {
            requests = new object[]
            {
                new
                {
                    namespaceKey = _settings.NamespaceKey,
                    flagKey = featureName,
                    entityId = ResolveEntityId(context),
                    context = context?.Attributes ?? new Dictionary<string, string>()
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning(
                "Flipt respondeu {StatusCode} ao avaliar feature {Feature}. Body: {Body}",
                (int)response.StatusCode,
                featureName,
                Truncate(responseBody, 300));
            return null;
        }

        return ParseBatchBooleanEvaluation(responseBody);
    }

    private static bool? ParseBatchBooleanEvaluation(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("responses", out var responses) ||
                responses.ValueKind != JsonValueKind.Array ||
                responses.GetArrayLength() == 0)
            {
                return null;
            }

            var first = responses[0];
            if (first.TryGetProperty("booleanResponse", out var booleanResponse) &&
                booleanResponse.ValueKind == JsonValueKind.Object &&
                booleanResponse.TryGetProperty("enabled", out var enabledElement) &&
                (enabledElement.ValueKind == JsonValueKind.True || enabledElement.ValueKind == JsonValueKind.False))
            {
                return enabledElement.GetBoolean();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ResolveEntityId(FliptContext? context)
    {
        if (!string.IsNullOrWhiteSpace(context?.EntityId))
        {
            return context.EntityId!;
        }

        throw new InvalidOperationException("entityId nao informado no contexto de avaliacao do Flipt");
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}