using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.Approvals;
using OpenPay.Application.DTOs.Payments;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using OpenPay.Infrastructure.Security;
namespace OpenPay.Web.Pages.Payments;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class EditModel : PageModel
{
    private readonly IPaymentOrderService _paymentOrderService;
    private readonly ICounterpartyService _counterpartyService;
    private readonly IOrganizationBankAccountService _accountService;
    private readonly IApprovalService _approvalService;
    private readonly UserManager<ApplicationUser> _userManager;
    public EditModel(
        IPaymentOrderService paymentOrderService,
        ICounterpartyService counterpartyService,
        IOrganizationBankAccountService accountService,
        IApprovalService approvalService, UserManager<ApplicationUser> userManager)
    {
        _paymentOrderService = paymentOrderService;
        _counterpartyService = counterpartyService;
        _accountService = accountService;
        _approvalService = approvalService;
        _userManager = userManager;
    }

    [BindProperty]
    public UpsertPaymentOrderDto Item { get; set; } = new();

    public List<SelectListItem> CounterpartyOptions { get; private set; } = [];
    public List<SelectListItem> AccountOptions { get; private set; } = [];

    public IReadOnlyList<ApprovalDecisionHistoryItemDto> ApprovalHistory { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var dto = await _paymentOrderService.GetByIdAsync(id);
        if (dto == null)
            return NotFound();

        Item = dto;
        await LoadOptionsAsync();
        ApprovalHistory = await _approvalService.GetHistoryAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOptionsAsync();

        if (Item.Id.HasValue)
        {
            ApprovalHistory = await _approvalService.GetHistoryAsync(Item.Id.Value);
        }

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();
            await _paymentOrderService.UpdateAsync(Item, userId);
            TempData["SuccessMessage"] = "Платежное поручение обновлено.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private async Task LoadOptionsAsync()
    {
        var counterparties = await _counterpartyService.GetAllAsync(null, true);
        var accounts = await _accountService.GetAllAsync(null, true);

        CounterpartyOptions = counterparties
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.FullName} ({x.Inn})"
            })
            .ToList();

        AccountOptions = accounts
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.BankName} / {x.AccountNumber}"
            })
            .ToList();
    }
}