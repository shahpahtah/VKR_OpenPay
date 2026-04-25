using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Admin.Organizations;

[Authorize(Roles = $"{nameof(UserRole.PlatformAdmin)}")]
public class CreateModel : PageModel
{
    private readonly IOrganizationManagementService _organizationManagementService;

    public CreateModel(IOrganizationManagementService organizationManagementService)
    {
        _organizationManagementService = organizationManagementService;
    }

    [BindProperty]
    public CreateOrganizationDto Item { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _organizationManagementService.CreateAsync(Item);
            TempData["SuccessMessage"] = "Организация и первый администратор успешно созданы.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}