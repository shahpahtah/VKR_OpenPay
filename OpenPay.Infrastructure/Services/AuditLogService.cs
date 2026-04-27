using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.DTOs.Audit;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly ICurrentOrganizationService _currentOrganizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(
        OpenPayDbContext dbContext,
        ICurrentOrganizationService currentOrganizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _currentOrganizationService = currentOrganizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        AuditEventType eventType,
        string? userId,
        string description,
        string? objectId = null,
        string? objectType = null)
    {
        var organizationId = await _currentOrganizationService.GetCurrentOrganizationIdAsync();

        var entity = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            EventType = eventType,
            UserId = userId,
            OrganizationId = organizationId,
            Description = description,
            ObjectId = objectId,
            ObjectType = objectType,
            IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
        };

        _dbContext.AuditLogEntries.Add(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLogListItemDto>> GetAllAsync(AuditLogFilterDto? filter = null)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var query =
            from log in _dbContext.AuditLogEntries.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking()
                on log.UserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            where log.OrganizationId == organizationId
            select new
            {
                log.CreatedAt,
                log.EventType,
                log.Description,
                log.ObjectId,
                log.ObjectType,
                log.IpAddress,
                log.UserId,
                UserName = user != null
                    ? (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email)
                    : null
            };

        if (filter != null)
        {
            if (filter.DateFrom.HasValue)
            {
                var localFrom = DateTime.SpecifyKind(filter.DateFrom.Value.Date, DateTimeKind.Unspecified);
                var utcFrom = TimeZoneInfo.ConvertTimeToUtc(localFrom, TimeZoneInfo.Local);

                query = query.Where(x => x.CreatedAt >= utcFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var localToExclusive = DateTime.SpecifyKind(filter.DateTo.Value.Date.AddDays(1), DateTimeKind.Unspecified);
                var utcToExclusive = TimeZoneInfo.ConvertTimeToUtc(localToExclusive, TimeZoneInfo.Local);

                query = query.Where(x => x.CreatedAt < utcToExclusive);
            }

            if (!string.IsNullOrWhiteSpace(filter.UserQuery))
            {
                var userQuery = filter.UserQuery.Trim();

                query = query.Where(x =>
                    (x.UserName != null && x.UserName.Contains(userQuery)) ||
                    (x.UserId != null && x.UserId.Contains(userQuery)));
            }

            if (!string.IsNullOrWhiteSpace(filter.EventType) &&
                Enum.TryParse<AuditEventType>(filter.EventType, out var eventType))
            {
                query = query.Where(x => x.EventType == eventType);
            }

            if (!string.IsNullOrWhiteSpace(filter.ObjectId))
            {
                var objectId = filter.ObjectId.Trim();
                query = query.Where(x => x.ObjectId != null && x.ObjectId.Contains(objectId));
            }
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AuditLogListItemDto
            {
                CreatedAt = x.CreatedAt,
                EventType = x.EventType.ToString(),
                UserName = x.UserName ?? "-",
                ObjectId = x.ObjectId,
                ObjectType = x.ObjectType,
                Description = x.Description,
                IpAddress = x.IpAddress
            })
            .ToListAsync();
    }
}