using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.ApprovalRoutes;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Security;
using OpenPay.Web.Common;

namespace OpenPay.Web.Pages.Admin.ApprovalRoutes;

[Authorize(Roles = $"{nameof(UserRole.Administrator)}")]
public class CreateModel : PageModel
{
    private readonly IApprovalRouteService _approvalRouteService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(
        IApprovalRouteService approvalRouteService,
        UserManager<ApplicationUser> userManager)
    {
        _approvalRouteService = approvalRouteService;
        _userManager = userManager;
    }

    [BindProperty]
    public UpsertApprovalRouteDto Item { get; set; } = new();

    public List<SelectListItem> ApprovalTypeOptions { get; private set; } = [];

    public void OnGet()
    {
        LoadOptions();
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
            await _approvalRouteService.CreateAsync(Item, userId);
            TempData["SuccessMessage"] = "Маршрут согласования создан.";
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
        ApprovalTypeOptions = Enum.GetValues<ApprovalType>()
            .Select(x => new SelectListItem
            {
                Value = x.ToString(),
                Text = UiText.ApprovalTypeText(x.ToString())
            })
            .ToList();
    }
}
