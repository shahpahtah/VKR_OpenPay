using System.ComponentModel.DataAnnotations;

namespace OpenPay.Application.DTOs.Approvals;

public class ApprovalActionDto
{
    public Guid PaymentOrderId { get; set; }

    [Display(Name = "Комментарий")]
    [StringLength(2000)]
    public string? Comment { get; set; }
}