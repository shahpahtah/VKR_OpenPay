using OpenPay.Domain.Enums;

namespace OpenPay.Domain.Entities;

public class PaymentOrder : BaseEntity
{
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaymentDate { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string ExpenseType { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; } = PaymentStatus.Draft;

    public Guid CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }

    public Guid OrganizationBankAccountId { get; set; }
    public OrganizationBankAccount? OrganizationBankAccount { get; set; }

    public Guid? ApprovalRouteId { get; set; }
    public ApprovalRoute? ApprovalRoute { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public ICollection<ApprovalDecision> ApprovalDecisions { get; set; } = new List<ApprovalDecision>();

    public string? BankReferenceId { get; set; }

    public string? BankResponseMessage { get; set; }

    public DateTime? SignedAt { get; set; }
    public string? SignatureReference { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
}
