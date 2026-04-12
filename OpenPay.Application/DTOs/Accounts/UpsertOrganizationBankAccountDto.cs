using System.ComponentModel.DataAnnotations;

namespace OpenPay.Application.DTOs.Accounts;

public class UpsertOrganizationBankAccountDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "БИК обязателен")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "БИК должен содержать 9 символов")]
    [Display(Name = "БИК")]
    public string Bic { get; set; } = string.Empty;

    [Required(ErrorMessage = "Номер счета обязателен")]
    [StringLength(20, MinimumLength = 20, ErrorMessage = "Номер счета должен содержать 20 символов")]
    [Display(Name = "Номер счета")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Наименование банка обязательно")]
    [StringLength(200)]
    [Display(Name = "Наименование банка")]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Валюта обязательна")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Код валюты должен содержать 3 символа")]
    [Display(Name = "Валюта")]
    public string Currency { get; set; } = "RUB";

    [Required(ErrorMessage = "Ответственное подразделение обязательно")]
    [Display(Name = "Ответственное подразделение")]
    public string ResponsibleUnit { get; set; } = string.Empty;

    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;
}