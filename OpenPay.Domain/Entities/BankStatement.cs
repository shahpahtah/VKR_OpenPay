namespace OpenPay.Domain.Entities;

public class BankStatement : BaseEntity
{
    public Guid OrganizationBankAccountId { get; set; }
    public OrganizationBankAccount? OrganizationBankAccount { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public string RawDataJson { get; set; } = string.Empty;
}
