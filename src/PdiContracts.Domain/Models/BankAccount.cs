namespace PdiContracts.Domain.Models;

/// <summary>
/// Representa uma conta bancária no domínio
/// </summary>
public class BankAccount
{
    public string Branch { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string AccountDigit { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Ispb { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
}
