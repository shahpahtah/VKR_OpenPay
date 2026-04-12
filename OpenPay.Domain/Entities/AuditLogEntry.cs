using OpenPay.Domain.Enums;

namespace OpenPay.Domain.Entities;

public class AuditLogEntry : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AuditEventType EventType { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string? ObjectId { get; set; }
    public string? ObjectType { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}