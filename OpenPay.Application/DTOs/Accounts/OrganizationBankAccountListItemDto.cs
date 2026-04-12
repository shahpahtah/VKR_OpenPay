namespace OpenPay.Application.DTOs.Accounts;

public class OrganizationBankAccountListItemDto
{
    public Guid Id { get; set; }
    public string Bic { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string ResponsibleUnit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}