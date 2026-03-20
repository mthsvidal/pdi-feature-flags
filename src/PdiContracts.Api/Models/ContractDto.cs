namespace PdiContracts.Api.Models;

public class ContractDto
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
    public BankAccountDto BankAccount { get; set; } = new();
    public List<ContractSpecificationDto> ContractSpecifications { get; set; } = new();
}
