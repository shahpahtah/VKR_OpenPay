using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Counterparties;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Counterparties;

[Authorize(Roles = $"{nameof(UserRole.Accountant)},{nameof(UserRole.Administrator)}")]
public class ImportModel : PageModel
{
    private readonly ICounterpartyService _counterpartyService;

    public ImportModel(ICounterpartyService counterpartyService)
    {
        _counterpartyService = counterpartyService;
    }

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    public CounterpartyImportResultDto? Result { get; private set; }

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

        try
        {
            await using var stream = UploadFile.OpenReadStream();
            Result = await _counterpartyService.ImportFromCsvAsync(stream);

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
