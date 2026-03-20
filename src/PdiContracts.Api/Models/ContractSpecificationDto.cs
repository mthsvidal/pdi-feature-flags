namespace PdiContracts.Api.Models;

public class ContractSpecificationDto
{
    public string ExpectedSettlementDate { get; set; } = string.Empty;
    public string OriginalAssetHolder { get; set; } = string.Empty;
    public string ReceivableDebtor { get; set; } = string.Empty;
    public string PaymentScheme { get; set; } = string.Empty;
    public string EffectValue { get; set; } = string.Empty;
    public string InitialExpectedSettlementDate { get; set; } = string.Empty;
    public string FinalExpectedSettlementDate { get; set; } = string.Empty;
}
