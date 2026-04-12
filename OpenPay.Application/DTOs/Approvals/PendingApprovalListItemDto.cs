namespace OpenPay.Application.DTOs.Approvals;

public class PendingApprovalListItemDto
{
    public Guid PaymentOrderId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string CounterpartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}