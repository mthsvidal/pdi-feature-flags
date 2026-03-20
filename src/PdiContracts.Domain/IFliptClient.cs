using PdiContracts.Domain.Models;

namespace PdiContracts.Domain;

/// <summary>
/// Interface para client de avaliacao de feature flags no Flipt.
/// </summary>
public interface IFliptClient
{
    /// <summary>
    /// Verifica se uma feature esta habilitada para o contexto especificado.
    /// </summary>
    /// <param name="featureName">Nome da feature flag</param>
    /// <param name="context">Contexto para avaliacao</param>
    /// <param name="defaultValue">Valor padrao caso a avaliacao falhe</param>
    /// <returns>True se a feature esta habilitada, false caso contrario</returns>
    Task<bool> IsEnabledAsync(string featureName, FliptContext? context = null, bool defaultValue = false);
}