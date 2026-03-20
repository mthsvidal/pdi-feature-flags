namespace PdiContracts.Domain.Models;

/// <summary>
/// Representa um contrato no domínio
/// </summary>
public class Contract
{
    public string Reference { get; set; } = string.Empty;
    public string ContractDueDate { get; set; } = string.Empty;
    public string AssetHolderDocumentType { get; set; } = string.Empty;
    public string AssetHolder { get; set; } = string.Empty;
    public string ContractUniqueIdentifier { get; set; } = string.Empty;
    public string SignatureDate { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    public string WarrantyType { get; set; } = string.Empty;
    public string WarrantyAmount { get; set; } = string.Empty;
    public string BalanceDue { get; set; } = string.Empty;
    public string DivisionMethod { get; set; } = string.Empty;
    public string EffectStrategy { get; set; } = string.Empty;
    public string PercentageValue { get; set; } = string.Empty;
    public BankAccount BankAccount { get; set; } = new();
    public List<ContractSpecification> ContractSpecifications { get; set; } = new();
}
