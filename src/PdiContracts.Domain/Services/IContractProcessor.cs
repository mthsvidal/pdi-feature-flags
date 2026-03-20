using PdiContracts.Domain.Models;

namespace PdiContracts.Domain.Services;

/// <summary>
/// Interface para processamento de contratos
/// </summary>
public interface IContractProcessor
{
    /// <summary>
    /// Processa um contrato realizando enriquecimento de dados
    /// </summary>
    /// <param name="request">Dados do contrato a processar</param>
    /// <returns>Resultado do processamento com dados enriquecidos</returns>
    Task<ContractProcessingResult> ProcessAsync(ContractRequest request);
}

/// <summary>
/// Resultado do processamento de contrato
/// </summary>
public class ContractProcessingResult
{
    /// <summary>
    /// Indica se o processamento foi bem-sucedido
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Contrato processado e enriquecido
    /// </summary>
    public ContractRequest? ProcessedContract { get; set; }

    /// <summary>
    /// Resumo do processamento
    /// </summary>
    public ProcessingSummary Summary { get; set; } = new();
}

/// <summary>
/// Resumo das informações processadas
/// </summary>
public class ProcessingSummary
{
    /// <summary>
    /// Total de Contract Specifications processadas
    /// </summary>
    public int TotalContractSpecifications { get; set; }

    /// <summary>
    /// Agrupamento de Contract Specifications por originalAssetHolder
    /// </summary>
    public List<AssetHolderGroup> SpecificationsByAssetHolder { get; set; } = new();

    /// <summary>
    /// Total de Contract Specifications notificadas
    /// </summary>
    public int TotalNotifiedContractSpecifications { get; set; }

    /// <summary>
    /// Data/hora de quando o processamento foi realizado
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Agrupamento de especificações por Asset Holder
/// </summary>
public class AssetHolderGroup
{
    /// <summary>
    /// Identificador do Asset Holder original
    /// </summary>
    public string OriginalAssetHolder { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade de Contract Specifications para este Asset Holder
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Quantidade de Contract Specifications notificadas para este Asset Holder
    /// </summary>
    public int NotifiedCount { get; set; }

}
