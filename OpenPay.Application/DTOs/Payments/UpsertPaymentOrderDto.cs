using OpenPay.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace OpenPay.Application.DTOs.Payments;

public class UpsertPaymentOrderDto
{
    public Guid? Id { get; set; }

    [Display(Name = "Номер документа")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Display(Name = "Дата платежа")]
    [DataType(DataType.Date)]
    public DateTime? PaymentDate { get; set; }

    [Required(ErrorMessage = "Необходимо выбрать контрагента")]
    [Display(Name = "Контрагент")]
    public Guid CounterpartyId { get; set; }

    [Required(ErrorMessage = "Необходимо выбрать счет организации")]
    [Display(Name = "Счет организации")]
    public Guid OrganizationBankAccountId { get; set; }

    [Required(ErrorMessage = "Сумма обязательна")]
    [Range(typeof(decimal), "0.01", "999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Сумма должна быть больше нуля")]
    [Display(Name = "Сумма")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Валюта обязательна")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Код валюты должен содержать 3 символа")]
    [Display(Name = "Валюта")]
    public string Currency { get; set; } = "RUB";

    [StringLength(100)]
    [Display(Name = "Тип расхода")]
    public string ExpenseType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Назначение платежа обязательно")]
    [StringLength(1000)]
    [Display(Name = "Назначение платежа")]
    public string Purpose { get; set; } = string.Empty;
    public PaymentStatus CurrentStatus { get; set; }

    public bool CanEdit =>
        CurrentStatus == PaymentStatus.Draft ||
        CurrentStatus == PaymentStatus.Rework;
    public string? BankReferenceId { get; set; }
    public string? BankResponseMessage { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureReference { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ApprovalRouteId { get; set; }
    public string? ApprovalRouteName { get; set; }
}
