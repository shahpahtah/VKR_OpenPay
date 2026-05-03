using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Admin.Users;

[Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.PlatformAdmin)}")]
public class EditModel : PageModel
{
    private readonly IUserManagementService _userManagementService;

    public EditModel(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [BindProperty]
    public UpdateUserDto Item { get; set; } = new();

    public List<SelectListItem> RoleOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        LoadRoles();

        var dto = await _userManagementService.GetByIdAsync(id);
        if (dto == null)
            return NotFound();

        Item = dto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadRoles();

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _userManagementService.UpdateAsync(Item);
            TempData["SuccessMessage"] = "Пользователь успешно обновлен.";
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
            .Where(x => x != UserRole.PlatformAdmin)
            .Select(x => new SelectListItem
            {
                Value = x.ToString(),
                Text = x.ToString()
            })
            .ToList();
    }
}
