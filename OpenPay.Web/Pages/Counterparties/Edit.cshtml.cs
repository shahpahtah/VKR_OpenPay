using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Counterparties;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Counterparties;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class EditModel : PageModel
{
    private readonly ICounterpartyService _counterpartyService;

    public EditModel(ICounterpartyService counterpartyService)
    {
        _counterpartyService = counterpartyService;
    }

    [BindProperty]
    public UpsertCounterpartyDto Item { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var dto = await _counterpartyService.GetByIdAsync(id);
        if (dto == null)
            return NotFound();

        Item = dto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _counterpartyService.UpdateAsync(Item);
            TempData["SuccessMessage"] = "Контрагент обновлен.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
