using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Reports;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Reports;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly IReportService _reportService;
    private readonly IReportExportService _reportExportService;

    public IndexModel(
        IReportService reportService,
        IReportExportService reportExportService)
    {
        _reportService = reportService;
        _reportExportService = reportExportService;
    }

    public ReportOverviewDto Report { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    public async Task OnGetAsync()
    {
        Report = await _reportService.GetOverviewAsync(DateFrom, DateTo);
    }

    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var report = await _reportService.GetOverviewAsync(DateFrom, DateTo);
        var bytes = _reportExportService.ExportToCsv(report);

        var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        var report = await _reportService.GetOverviewAsync(DateFrom, DateTo);
        var bytes = _reportExportService.ExportToExcel(report);

        var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}