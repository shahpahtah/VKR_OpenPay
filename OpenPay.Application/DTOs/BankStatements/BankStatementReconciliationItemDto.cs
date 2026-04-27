namespace OpenPay.Application.DTOs.BankStatements;

public class BankStatementReconciliationItemDto
{
    public string OperationId { get; set; } = string.Empty;
    public DateOnly OperationDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string CounterpartyName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? BankReferenceId { get; set; }
    public Guid? MatchedPaymentOrderId { get; set; }
    public string? MatchedDocumentNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}
