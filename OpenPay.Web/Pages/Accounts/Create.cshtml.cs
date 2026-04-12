using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Accounts;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
namespace OpenPay.Web.Pages.Accounts;
[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class CreateModel : PageModel
{
    private readonly IOrganizationBankAccountService _accountService;

    public CreateModel(IOrganizationBankAccountService accountService)
    {
        _accountService = accountService;
    }

    [BindProperty]
    public UpsertOrganizationBankAccountDto Item { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _accountService.CreateAsync(Item);
            TempData["SuccessMessage"] = "Банковский счет успешно создан.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}