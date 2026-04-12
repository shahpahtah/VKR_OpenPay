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

    public IndexModel(IReportService reportService)
    {
        _reportService = reportService;
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
}