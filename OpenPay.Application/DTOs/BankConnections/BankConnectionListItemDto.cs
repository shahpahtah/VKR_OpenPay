namespace OpenPay.Application.DTOs.BankConnections;

public class BankConnectionListItemDto
{
    public Guid Id { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankDisplayName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool HasAccessToken { get; set; }
    public bool HasRefreshToken { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
