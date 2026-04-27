namespace OpenPay.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
    public string Kpp { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Counterparty> Counterparties { get; set; } = new List<Counterparty>();
    public ICollection<OrganizationBankAccount> BankAccounts { get; set; } = new List<OrganizationBankAccount>();
    public ICollection<BankConnection> BankConnections { get; set; } = new List<BankConnection>();
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
    public ICollection<ApprovalRoute> ApprovalRoutes { get; set; } = new List<ApprovalRoute>();
    public ICollection<AuditLogEntry> AuditLogEntries { get; set; } = new List<AuditLogEntry>();
}
