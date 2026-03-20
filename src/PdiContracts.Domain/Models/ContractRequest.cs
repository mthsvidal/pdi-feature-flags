namespace PdiContracts.Domain.Models;

/// <summary>
/// Representa uma requisição de contrato no domínio
/// </summary>
public class ContractRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<Contract> Contracts { get; set; } = new();
}
