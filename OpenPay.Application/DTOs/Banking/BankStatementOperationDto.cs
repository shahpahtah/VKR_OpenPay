namespace OpenPay.Application.DTOs.Banking;

public class BankStatementOperationDto
{
    public string OperationId { get; set; } = string.Empty;
    public DateOnly OperationDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string CounterpartyName { get; set; } = string.Empty;
    public string CounterpartyAccountNumber { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? BankReferenceId { get; set; }
}
