using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Accounts;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
namespace OpenPay.Web.Pages.Accounts;


[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly IOrganizationBankAccountService _accountService;

    public IndexModel(IOrganizationBankAccountService accountService)
    {
        _accountService = accountService;
    }

    public IReadOnlyList<OrganizationBankAccountListItemDto> Items { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    public async Task OnGetAsync()
    {
        bool? isActive = ShowInactive ? null : true;
        Items = await _accountService.GetAllAsync(Search, isActive);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _accountService.DeactivateAsync(id);
        return RedirectToPage(new { Search, ShowInactive });
    }
}
