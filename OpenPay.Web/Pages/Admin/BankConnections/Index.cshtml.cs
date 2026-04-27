using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.BankConnections;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Web.Pages.Admin.BankConnections;

[Authorize(Roles = $"{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly IBankConnectionService _bankConnectionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        IBankConnectionService bankConnectionService,
        UserManager<ApplicationUser> userManager)
    {
        _bankConnectionService = bankConnectionService;
        _userManager = userManager;
    }

    public IReadOnlyList<BankConnectionListItemDto> Items { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    public async Task OnGetAsync()
    {
        Items = await _bankConnectionService.GetAllAsync(ShowInactive);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _bankConnectionService.DeactivateAsync(id, userId);
            TempData["SuccessMessage"] = "Банковское подключение деактивировано.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { ShowInactive });
    }
}
