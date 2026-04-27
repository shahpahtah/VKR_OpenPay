namespace OpenPay.Application.DTOs.Reports;

public class CounterpartySummaryDto
{
    public string CounterpartyName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}
