using System.ComponentModel.DataAnnotations;

namespace OpenPay.Application.DTOs.BankConnections;

public class UpsertBankConnectionDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Выберите банк")]
    [Display(Name = "Банк")]
    public string BankCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Название подключения обязательно")]
    [StringLength(200)]
    [Display(Name = "Название подключения")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Access token")]
    public string? AccessToken { get; set; }

    [StringLength(2000)]
    [Display(Name = "Refresh token")]
    public string? RefreshToken { get; set; }

    [Display(Name = "Активно")]
    public bool IsActive { get; set; } = true;

    public bool HasAccessToken { get; set; }
    public bool HasRefreshToken { get; set; }
}
