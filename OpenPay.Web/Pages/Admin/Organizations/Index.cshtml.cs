using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Admin.Organizations;

[Authorize(Roles = $"{nameof(UserRole.PlatformAdmin)}")]
public class IndexModel : PageModel
{
    private readonly IOrganizationManagementService _organizationManagementService;

    public IndexModel(IOrganizationManagementService organizationManagementService)
    {
        _organizationManagementService = organizationManagementService;
    }

    public IReadOnlyList<OrganizationListItemDto> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Items = await _organizationManagementService.GetAllAsync();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        try
        {
            await _organizationManagementService.DeactivateAsync(id);
            TempData["SuccessMessage"] = "Организация деактивирована.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        try
        {
            await _organizationManagementService.ActivateAsync(id);
            TempData["SuccessMessage"] = "Организация активирована.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }
}
