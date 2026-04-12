using OpenPay.Application.DTOs.Reports;

public class ReportOverviewDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public int TotalPaymentsCount { get; set; }
    public decimal TotalPaymentsAmount { get; set; }

    public int ExecutedPaymentsCount { get; set; }
    public decimal ExecutedPaymentsAmount { get; set; }

    public int PendingApprovalCount { get; set; }
    public decimal PendingApprovalAmount { get; set; }

    public int ErrorPaymentsCount { get; set; }
    public decimal ErrorPaymentsAmount { get; set; }

    public IReadOnlyList<StatusSummaryDto> StatusSummary { get; set; } = [];
    public IReadOnlyList<PaymentReportItemDto> Items { get; set; } = [];
}