using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.ApprovalRoutes;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class ApprovalRouteService : IApprovalRouteService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentOrganizationService _currentOrganizationService;

    public ApprovalRouteService(
        OpenPayDbContext dbContext,
        IAuditLogService auditLogService,
        ICurrentOrganizationService currentOrganizationService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _currentOrganizationService = currentOrganizationService;
    }

    public async Task<IReadOnlyList<ApprovalRouteListItemDto>> GetAllAsync(bool includeInactive = false)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var query = _dbContext.ApprovalRoutes
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        var routes = await query
            .Select(x => new ApprovalRouteListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                MinAmount = x.MinAmount,
                MaxAmount = x.MaxAmount,
                ExpenseType = x.ExpenseType,
                Department = x.Department,
                ApprovalType = x.ApprovalType,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return routes
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.MinAmount ?? 0m)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public async Task<UpsertApprovalRouteDto?> GetByIdAsync(Guid id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.ApprovalRoutes
            .AsNoTracking()
            .Where(x => x.Id == id && x.OrganizationId == organizationId)
            .Select(x => new UpsertApprovalRouteDto
            {
                Id = x.Id,
                Name = x.Name,
                MinAmount = x.MinAmount,
                MaxAmount = x.MaxAmount,
                ExpenseType = x.ExpenseType,
                Department = x.Department,
                ApprovalType = x.ApprovalType,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(UpsertApprovalRouteDto dto, string userId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        Validate(dto);

        var entity = new ApprovalRoute
        {
            OrganizationId = organizationId,
            Name = dto.Name.Trim(),
            MinAmount = dto.MinAmount,
            MaxAmount = dto.MaxAmount,
            ExpenseType = Normalize(dto.ExpenseType),
            Department = Normalize(dto.Department),
            ApprovalType = dto.ApprovalType,
            IsActive = dto.IsActive
        };

        _dbContext.ApprovalRoutes.Add(entity);
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.ApprovalRouteCreated,
            userId,
            $"Создан маршрут согласования {entity.Name}",
            entity.Id.ToString(),
            nameof(ApprovalRoute));

        return entity.Id;
    }

    public async Task UpdateAsync(UpsertApprovalRouteDto dto, string userId)
    {
        if (dto.Id == null || dto.Id == Guid.Empty)
            throw new InvalidOperationException("Идентификатор маршрута не указан.");

        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        Validate(dto);

        var entity = await _dbContext.ApprovalRoutes
            .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.OrganizationId == organizationId);

        if (entity == null)
            throw new InvalidOperationException("Маршрут согласования не найден.");

        entity.Name = dto.Name.Trim();
        entity.MinAmount = dto.MinAmount;
        entity.MaxAmount = dto.MaxAmount;
        entity.ExpenseType = Normalize(dto.ExpenseType);
        entity.Department = Normalize(dto.Department);
        entity.ApprovalType = dto.ApprovalType;
        entity.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.ApprovalRouteUpdated,
            userId,
            $"Обновлен маршрут согласования {entity.Name}",
            entity.Id.ToString(),
            nameof(ApprovalRoute));
    }

    public async Task DeactivateAsync(Guid id, string userId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var entity = await _dbContext.ApprovalRoutes
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId);

        if (entity == null)
            return;

        entity.IsActive = false;
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.ApprovalRouteDeactivated,
            userId,
            $"Деактивирован маршрут согласования {entity.Name}",
            entity.Id.ToString(),
            nameof(ApprovalRoute));
    }

    private static void Validate(UpsertApprovalRouteDto dto)
    {
        if (dto.MinAmount.HasValue && dto.MaxAmount.HasValue && dto.MinAmount > dto.MaxAmount)
            throw new InvalidOperationException("Минимальная сумма не может быть больше максимальной.");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
