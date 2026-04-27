namespace OpenPay.Domain.Entities;

public class BankConnection : BaseEntity
{
    public string BankCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ProtectedAccessToken { get; set; } = string.Empty;
    public string ProtectedRefreshToken { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public ICollection<OrganizationBankAccount> BankAccounts { get; set; } = new List<OrganizationBankAccount>();
}
