using OpenPay.Application.DTOs.Reports;

namespace OpenPay.Application.Interfaces;

public interface IReportService
{
    Task<ReportOverviewDto> GetOverviewAsync(DateTime? dateFrom, DateTime? dateTo);
}