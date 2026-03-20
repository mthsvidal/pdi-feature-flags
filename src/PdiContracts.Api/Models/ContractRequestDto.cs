namespace PdiContracts.Api.Models;

public class ContractRequestDto
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<ContractDto> Contracts { get; set; } = new();
}
