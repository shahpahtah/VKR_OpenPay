using OpenPay.Application.DTOs.Reports;

namespace OpenPay.Application.Interfaces;

public interface IReportExportService
{
    byte[] ExportToCsv(ReportOverviewDto report);
    byte[] ExportToExcel(ReportOverviewDto report);
}