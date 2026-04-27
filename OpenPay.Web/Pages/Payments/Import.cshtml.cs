using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Payments;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Web.Pages.Payments;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class ImportModel : PageModel
{
    private readonly IPaymentOrderService _paymentOrderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ImportModel(
        IPaymentOrderService paymentOrderService,
        UserManager<ApplicationUser> userManager)
    {
        _paymentOrderService = paymentOrderService;
        _userManager = userManager;
    }

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    public PaymentOrderImportResultDto? Result { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadFile == null || UploadFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Выберите CSV-файл для загрузки.");
            return Page();
        }

        if (!Path.GetExtension(UploadFile.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Допустим только CSV-файл.");
            return Page();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await using var stream = UploadFile.OpenReadStream();
            Result = await _paymentOrderService.ImportFromCsvAsync(stream, userId);

            TempData["SuccessMessage"] =
                $"Импорт завершен. Успешно: {Result.ImportedRows}, с ошибками: {Result.ErrorRows}.";
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return Page();
    }
}
