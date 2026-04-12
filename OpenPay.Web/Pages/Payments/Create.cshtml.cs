using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.Payments;
using OpenPay.Application.Interfaces;
using OpenPay.Infrastructure.Security;
using OpenPay.Domain.Enums;
namespace OpenPay.Web.Pages.Payments;
[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class CreateModel : PageModel
{
    private readonly IPaymentOrderService _paymentOrderService;
    private readonly ICounterpartyService _counterpartyService;
    private readonly IOrganizationBankAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(
        IPaymentOrderService paymentOrderService,
        ICounterpartyService counterpartyService,
        IOrganizationBankAccountService accountService,
        UserManager<ApplicationUser> userManager)
    {
        _paymentOrderService = paymentOrderService;
        _counterpartyService = counterpartyService;
        _accountService = accountService;
        _userManager = userManager;
    }

    [BindProperty]
    public UpsertPaymentOrderDto Item { get; set; } = new();

    public List<SelectListItem> CounterpartyOptions { get; private set; } = [];
    public List<SelectListItem> AccountOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOptionsAsync();

        if (!ModelState.IsValid)
            return Page();

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _paymentOrderService.CreateAsync(Item, userId);
            TempData["SuccessMessage"] = "Платежное поручение сохранено как черновик.";
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