namespace OpenPay.Application.DTOs.Counterparties;

public class CounterpartyListItemDto
{
    public Guid Id { get; set; }
    public string Inn { get; set; } = string.Empty;
    public string Kpp { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Bic { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}