using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Admin.Users;

[Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.PlatformAdmin)}")]
public class IndexModel : PageModel
{
    private readonly IUserManagementService _userManagementService;

    public IndexModel(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public IReadOnlyList<UserListItemDto> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Items = await _userManagementService.GetUsersForCurrentOrganizationAsync();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(string id)
    {
        try
        {
            await _userManagementService.DeactivateAsync(id);
            TempData["SuccessMessage"] = "Пользователь деактивирован.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(string id)
    {
        try
        {
            await _userManagementService.ActivateAsync(id);
            TempData["SuccessMessage"] = "Пользователь активирован.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }
}
