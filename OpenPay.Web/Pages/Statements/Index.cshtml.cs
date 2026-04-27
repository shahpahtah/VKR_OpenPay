using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.BankStatements;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Web.Pages.Statements;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly IBankStatementService _bankStatementService;
    private readonly IOrganizationBankAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        IBankStatementService bankStatementService,
        IOrganizationBankAccountService accountService,
        UserManager<ApplicationUser> userManager)
    {
        _bankStatementService = bankStatementService;
        _accountService = accountService;
        _userManager = userManager;
    }

    public IReadOnlyList<BankStatementListItemDto> Statements { get; private set; } = [];
    public BankStatementResultDto? Result { get; private set; }
    public List<SelectListItem> AccountOptions { get; private set; } = [];

    [BindProperty]
    public Guid OrganizationBankAccountId { get; set; }

    [BindProperty]
    public DateTime DateFrom { get; set; } = DateTime.Today.AddDays(-7);

    [BindProperty]
    public DateTime DateTo { get; set; } = DateTime.Today;

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostLoadAsync()
    {
        await LoadAsync();

        if (!ModelState.IsValid)
            return Page();

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            Result = await _bankStatementService.LoadDemoStatementAsync(
                OrganizationBankAccountId,
                DateOnly.FromDateTime(DateFrom),
                DateOnly.FromDateTime(DateTo),
                userId);
            TempData["SuccessMessage"] = "Демо-выписка загружена и сверена.";
            Statements = await _bankStatementService.GetAllAsync();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReconcileAsync(Guid id)
    {
        await LoadAsync();

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            Result = await _bankStatementService.ReconcileAsync(id, userId);
            TempData["SuccessMessage"] = "Сверка выписки выполнена.";
            Statements = await _bankStatementService.GetAllAsync();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return Page();
    }

    private async Task LoadAsync()
    {
        var accounts = await _accountService.GetAllAsync(null, true);
        AccountOptions = accounts
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.BankName} / {x.AccountNumber} / {x.BankConnectionDisplay}"
            })
            .ToList();

        Statements = await _bankStatementService.GetAllAsync();
    }
}
