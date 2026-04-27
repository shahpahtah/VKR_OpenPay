using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.BankConnections;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Web.Pages.Admin.BankConnections;

[Authorize(Roles = $"{nameof(UserRole.Administrator)}")]
public class EditModel : PageModel
{
    private readonly IBankConnectionService _bankConnectionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(
        IBankConnectionService bankConnectionService,
        UserManager<ApplicationUser> userManager)
    {
        _bankConnectionService = bankConnectionService;
        _userManager = userManager;
    }

    [BindProperty]
    public UpsertBankConnectionDto Item { get; set; } = new();

    public List<SelectListItem> BankOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        LoadOptions();

        var dto = await _bankConnectionService.GetByIdAsync(id);
        if (dto == null)
            return NotFound();

        Item = dto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadOptions();

        if (!ModelState.IsValid)
            return Page();

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _bankConnectionService.UpdateAsync(Item, userId);
            TempData["SuccessMessage"] = "Банковское подключение обновлено.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private void LoadOptions()
    {
        BankOptions = _bankConnectionService.GetAvailableBanks()
            .Select(x => new SelectListItem { Value = x.BankCode, Text = x.DisplayName })
            .ToList();
    }
}
