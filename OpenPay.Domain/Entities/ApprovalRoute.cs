using OpenPay.Domain.Enums;

namespace OpenPay.Domain.Entities;

public class ApprovalRoute : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? ExpenseType { get; set; }
    public string? Department { get; set; }
    public ApprovalType ApprovalType { get; set; } = ApprovalType.Sequential;
    public bool IsActive { get; set; } = true;

    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
}