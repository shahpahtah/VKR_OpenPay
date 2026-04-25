using System.ComponentModel.DataAnnotations;
using OpenPay.Domain.Enums;

namespace OpenPay.Application.DTOs.Admin;

public class UpdateUserDto
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "ФИО обязательно")]
    [StringLength(200)]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Роль обязательна")]
    [Display(Name = "Роль")]
    public UserRole Role { get; set; }

    [Display(Name = "Активен")]
    public bool IsActive { get; set; }
}