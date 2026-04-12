using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Calendar;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Calendar;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly ICalendarService _calendarService;

    public IndexModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    public IReadOnlyList<CalendarDayDto> Days { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    public async Task OnGetAsync()
    {
        DateFrom ??= DateTime.Today;
        DateTo ??= DateTime.Today.AddDays(30);

        Days = await _calendarService.GetCalendarAsync(DateFrom, DateTo);
    }
}