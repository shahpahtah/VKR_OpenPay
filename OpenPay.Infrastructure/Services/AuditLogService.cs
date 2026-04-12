using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Audit;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly OpenPayDbContext _dbContext;

    public AuditLogService(OpenPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(
        AuditEventType eventType,
        string userId,
        string description,
        string? objectId = null,
        string? objectType = null,
        string? ipAddress = null)
    {
        var entry = new AuditLogEntry
        {
            EventType = eventType,
            UserId = userId,
            Description = description,
            ObjectId = objectId,
            ObjectType = objectType,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AuditLogEntries.Add(entry);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLogListItemDto>> GetRecentAsync(int take = 100)
    {
        var result = await
            (from log in _dbContext.AuditLogEntries.AsNoTracking()
             join user in _dbContext.Users.AsNoTracking()
                 on log.UserId equals user.Id into users
             from user in users.DefaultIfEmpty()
             orderby log.CreatedAt descending
             select new AuditLogListItemDto
             {
                 CreatedAt = log.CreatedAt,
                 EventType = log.EventType.ToString(),
                 UserName = user != null ? user.FullName : log.UserId,
                 ObjectType = log.ObjectType,
                 ObjectId = log.ObjectId,
                 Description = log.Description,
                 IpAddress = log.IpAddress
             })
            .Take(take)
            .ToListAsync();

        return result;
    }
}