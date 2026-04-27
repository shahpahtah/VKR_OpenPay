using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Payments;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Payments;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly IPaymentOrderService _paymentOrderService;
    private readonly IApprovalService _approvalService;
    private readonly IBankProcessingService _bankProcessingService;

    public IndexModel(
        IPaymentOrderService paymentOrderService,
        IApprovalService approvalService,
        IBankProcessingService bankProcessingService)
    {
        _paymentOrderService = paymentOrderService;
        _approvalService = approvalService;
        _bankProcessingService = bankProcessingService;
    }

    public IReadOnlyList<PaymentOrderListItemDto> Items { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync()
    {
        Items = await _paymentOrderService.GetAllAsync(Search);
    }

    public async Task<IActionResult> OnPostSubmitForApprovalAsync(Guid id)
    {
        try
        {
            await _approvalService.SubmitForApprovalAsync(id);
            TempData["SuccessMessage"] = "Платеж отправлен на согласование.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { Search });
    }

    public async Task<IActionResult> OnPostSendToBankAsync(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _bankProcessingService.SendToBankAsync(id, userId);
            TempData["SuccessMessage"] = "Платеж подписан и отправлен в банк.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { Search });
    }
}
