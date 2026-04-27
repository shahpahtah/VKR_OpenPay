namespace OpenPay.Application.DTOs.Admin;

public class AuditLogFilterDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? UserQuery { get; set; }
    public string? EventType { get; set; }
    public string? ObjectId { get; set; }
}