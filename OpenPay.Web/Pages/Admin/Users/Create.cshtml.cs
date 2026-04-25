using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Admin.Users;

[Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.PlatformAdmin)}")]
public class CreateModel : PageModel
{
    private readonly IUserManagementService _userManagementService;

    public CreateModel(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [BindProperty]
    public CreateUserDto Item { get; set; } = new();

    public List<SelectListItem> RoleOptions { get; private set; } = [];

    public void OnGet()
    {
        LoadRoles();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadRoles();

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _userManagementService.CreateUserAsync(Item);
            TempData["SuccessMessage"] = "Сотрудник успешно создан.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private void LoadRoles()
    {
        RoleOptions = Enum.GetValues<UserRole>()
            .Select(x => new SelectListItem
            {
                Value = x.ToString(),
                Text = x.ToString()
            })
            .ToList();
    }
}