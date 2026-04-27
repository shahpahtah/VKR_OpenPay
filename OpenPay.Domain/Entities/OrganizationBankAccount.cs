namespace OpenPay.Domain.Entities;

public class OrganizationBankAccount : BaseEntity
{
    public string Bic { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Currency { get; set; } = "RUB";
    public string ResponsibleUnit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Guid? BankConnectionId { get; set; }
    public BankConnection? BankConnection { get; set; }

    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
}
