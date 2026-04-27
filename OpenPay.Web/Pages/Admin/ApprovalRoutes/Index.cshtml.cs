using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.ApprovalRoutes;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Web.Pages.Admin.ApprovalRoutes;

[Authorize(Roles = $"{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly IApprovalRouteService _approvalRouteService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        IApprovalRouteService approvalRouteService,
        UserManager<ApplicationUser> userManager)
    {
        _approvalRouteService = approvalRouteService;
        _userManager = userManager;
    }

    public IReadOnlyList<ApprovalRouteListItemDto> Items { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    public async Task OnGetAsync()
    {
        Items = await _approvalRouteService.GetAllAsync(ShowInactive);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _approvalRouteService.DeactivateAsync(id, userId);
            TempData["SuccessMessage"] = "Маршрут согласования деактивирован.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { ShowInactive });
    }
}
