using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.DTOs.Audit;
using OpenPay.Domain.Enums;

namespace OpenPay.Application.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        AuditEventType eventType,
        string? userId,
        string description,
        string? objectId = null,
        string? objectType = null);

    Task<IReadOnlyList<AuditLogListItemDto>> GetAllAsync(AuditLogFilterDto? filter = null);
}