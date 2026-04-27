using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.Accounts;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Accounts;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class CreateModel : PageModel
{
    private readonly IOrganizationBankAccountService _accountService;
    private readonly IBankConnectionService _bankConnectionService;

    public CreateModel(
        IOrganizationBankAccountService accountService,
        IBankConnectionService bankConnectionService)
    {
        _accountService = accountService;
        _bankConnectionService = bankConnectionService;
    }

    [BindProperty]
    public UpsertOrganizationBankAccountDto Item { get; set; } = new();

    public List<SelectListItem> BankConnectionOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOptionsAsync();

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _accountService.CreateAsync(Item);
            TempData["SuccessMessage"] = "Банковский счет создан.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private async Task LoadOptionsAsync()
    {
        var connections = await _bankConnectionService.GetAllAsync();
        BankConnectionOptions = connections
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.DisplayName} / {x.BankDisplayName}"
            })
            .ToList();
    }
}
