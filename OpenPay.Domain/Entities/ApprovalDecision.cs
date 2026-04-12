using OpenPay.Domain.Enums;

namespace OpenPay.Domain.Entities;

public class ApprovalDecision : BaseEntity
{
    public Guid PaymentOrderId { get; set; }
    public PaymentOrder? PaymentOrder { get; set; }

    public string ApproverUserId { get; set; } = string.Empty;

    public ApprovalDecisionType Decision { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}