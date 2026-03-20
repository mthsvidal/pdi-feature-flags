namespace PdiContracts.Api.Models;

public class ContractResponseDto
{
    public List<ContractKeyDto> Contracts { get; set; } = new();
    public string ProcessKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ContractKeyDto
{
    public string Key { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}
