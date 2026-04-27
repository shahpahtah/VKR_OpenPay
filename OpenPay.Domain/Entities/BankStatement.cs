namespace OpenPay.Domain.Entities;

public class BankStatement : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid OrganizationBankAccountId { get; set; }
    public OrganizationBankAccount? OrganizationBankAccount { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public string RawDataJson { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int MatchedOperations { get; set; }
    public int UnmatchedOperations { get; set; }

    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
}
