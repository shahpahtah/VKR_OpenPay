namespace OpenPay.Application.DTOs.Audit;

public class AuditLogListItemDto
{
    public DateTime CreatedAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ObjectType { get; set; }
    public string? ObjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}