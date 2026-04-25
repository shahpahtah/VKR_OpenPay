using System.ComponentModel.DataAnnotations;

namespace OpenPay.Application.DTOs.Admin;

public class CreateOrganizationDto
{
    [Required(ErrorMessage = "Название организации обязательно")]
    [StringLength(300)]
    [Display(Name = "Название организации")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "ИНН обязателен")]
    [StringLength(12, MinimumLength = 10, ErrorMessage = "ИНН должен содержать 10 или 12 символов")]
    [Display(Name = "ИНН")]
    public string Inn { get; set; } = string.Empty;

    [Required(ErrorMessage = "КПП обязателен")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "КПП должен содержать 9 символов")]
    [Display(Name = "КПП")]
    public string Kpp { get; set; } = string.Empty;

    [Required(ErrorMessage = "ФИО администратора обязательно")]
    [StringLength(200)]
    [Display(Name = "ФИО администратора")]
    public string AdminFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    [Display(Name = "Email администратора")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string AdminPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтверждение пароля обязательно")]
    [Compare(nameof(AdminPassword), ErrorMessage = "Пароли не совпадают")]
    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    public string ConfirmAdminPassword { get; set; } = string.Empty;
}