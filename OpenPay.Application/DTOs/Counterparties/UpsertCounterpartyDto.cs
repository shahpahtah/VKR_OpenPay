using System.ComponentModel.DataAnnotations;

namespace OpenPay.Application.DTOs.Counterparties;

public class UpsertCounterpartyDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "ИНН обязателен")]
    [StringLength(12, MinimumLength = 10, ErrorMessage = "ИНН должен содержать 10 или 12 символов")]
    [Display(Name = "ИНН")]
    public string Inn { get; set; } = string.Empty;

    [Required(ErrorMessage = "КПП обязателен")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "КПП должен содержать 9 символов")]
    [Display(Name = "КПП")]
    public string Kpp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Полное наименование обязательно")]
    [StringLength(500)]
    [Display(Name = "Полное наименование")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "БИК обязателен")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "БИК должен содержать 9 символов")]
    [Display(Name = "БИК")]
    public string Bic { get; set; } = string.Empty;

    [Required(ErrorMessage = "Расчетный счет обязателен")]
    [StringLength(20, MinimumLength = 20, ErrorMessage = "Счет должен содержать 20 символов")]
    [Display(Name = "Расчетный счет")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Корреспондентский счет обязателен")]
    [StringLength(20, MinimumLength = 20, ErrorMessage = "Корреспондентский счет должен содержать 20 символов")]
    [Display(Name = "Корреспондентский счет")]
    public string CorrespondentAccount { get; set; } = string.Empty;

    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;
}