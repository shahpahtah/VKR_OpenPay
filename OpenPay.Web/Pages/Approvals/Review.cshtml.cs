using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Approvals;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Web.Pages.Approvals;

[Authorize(Roles = $"{nameof(UserRole.Manager)},{nameof(UserRole.Administrator)}")]
public class ReviewModel : PageModel
{
    private readonly IApprovalService _approvalService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewModel(
        IApprovalService approvalService,
        UserManager<ApplicationUser> userManager)
    {
        _approvalService = approvalService;
        _userManager = userManager;
    }

    public ApprovalReviewDto? Item { get; private set; }

    [BindProperty]
    public ApprovalActionDto ActionModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Item = await _approvalService.GetReviewModelAsync(id);

        if (Item == null)
            return NotFound();

        ActionModel.PaymentOrderId = id;
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        return await ExecuteAsync((paymentId, userId, comment) =>
            _approvalService.ApproveAsync(paymentId, userId, comment));
    }

    public async Task<IActionResult> OnPostRejectAsync()
    {
        return await ExecuteAsync((paymentId, userId, comment) =>
            _approvalService.RejectAsync(paymentId, userId, comment ?? string.Empty));
    }

    public async Task<IActionResult> OnPostReworkAsync()
    {
        return await ExecuteAsync((paymentId, userId, comment) =>
            _approvalService.ReturnForReworkAsync(paymentId, userId, comment ?? string.Empty));
    }

    private async Task<IActionResult> ExecuteAsync(Func<Guid, string, string?, Task> action)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await action(ActionModel.PaymentOrderId, userId, ActionModel.Comment);
            TempData["SuccessMessage"] = "Решение по платежу сохранено.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            Item = await _approvalService.GetReviewModelAsync(ActionModel.PaymentOrderId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}