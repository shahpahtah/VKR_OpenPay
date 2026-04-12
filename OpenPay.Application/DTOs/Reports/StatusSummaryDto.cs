namespace OpenPay.Application.DTOs.Reports;

public class StatusSummaryDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}