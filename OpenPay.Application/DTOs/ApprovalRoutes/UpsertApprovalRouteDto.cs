using System.ComponentModel.DataAnnotations;
using OpenPay.Domain.Enums;

namespace OpenPay.Application.DTOs.ApprovalRoutes;

public class UpsertApprovalRouteDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Название маршрута обязательно")]
    [StringLength(200)]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999999.99", ParseLimitsInInvariantCulture = true)]
    [Display(Name = "Сумма от")]
    public decimal? MinAmount { get; set; }

    [Range(typeof(decimal), "0", "999999999999.99", ParseLimitsInInvariantCulture = true)]
    [Display(Name = "Сумма до")]
    public decimal? MaxAmount { get; set; }

    [StringLength(100)]
    [Display(Name = "Тип расхода")]
    public string? ExpenseType { get; set; }

    [StringLength(200)]
    [Display(Name = "Подразделение")]
    public string? Department { get; set; }

    [Display(Name = "Тип согласования")]
    public ApprovalType ApprovalType { get; set; } = ApprovalType.Sequential;

    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;
}
