namespace PdiContracts.Domain.Services;

/// <summary>
/// Interface para serviço de feature flags
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Verifica se uma feature está habilitada com base em um contexto JSON
    /// </summary>
    /// <param name="featureName">Nome da feature flag</param>
    /// <param name="contextJson">JSON com propriedades do contexto (todos os campos serão achatados em Properties)</param>
    /// <param name="defaultValue">Valor padrão caso a feature não seja encontrada</param>
    /// <returns>True se a feature está habilitada, false caso contrário</returns>
    Task<bool> IsEnabledAsync(string featureName, string? contextJson = null, bool defaultValue = false);
}
