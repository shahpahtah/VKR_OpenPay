namespace OpenPay.Application.DTOs.Reports;

public class PaymentReportItemDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaymentDate { get; set; }

    public string CounterpartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}