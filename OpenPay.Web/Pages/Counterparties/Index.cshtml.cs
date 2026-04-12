using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Counterparties;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
namespace OpenPay.Web.Pages.Counterparties;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly ICounterpartyService _counterpartyService;

    public IndexModel(ICounterpartyService counterpartyService)
    {
        _counterpartyService = counterpartyService;
    }

    public IReadOnlyList<CounterpartyListItemDto> Items { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    public async Task OnGetAsync()
    {
        bool? isActive = ShowInactive ? null : true;
        Items = await _counterpartyService.GetAllAsync(Search, isActive);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _counterpartyService.DeactivateAsync(id);
        return RedirectToPage(new { Search, ShowInactive });
    }
}