using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Counterparties;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
namespace OpenPay.Web.Pages.Counterparties;
[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class CreateModel : PageModel
{
    private readonly ICounterpartyService _counterpartyService;

    public CreateModel(ICounterpartyService counterpartyService)
    {
        _counterpartyService = counterpartyService;
    }

    [BindProperty]
    public UpsertCounterpartyDto Item { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _counterpartyService.CreateAsync(Item);
            TempData["SuccessMessage"] = "Контрагент успешно создан.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}