using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.BankConnections;
using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class BankConnectionService : IBankConnectionService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly IBankAdapterRegistry _bankAdapterRegistry;
    private readonly ITokenProtectionService _tokenProtectionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentOrganizationService _currentOrganizationService;

    public BankConnectionService(
        OpenPayDbContext dbContext,
        IBankAdapterRegistry bankAdapterRegistry,
        ITokenProtectionService tokenProtectionService,
        IAuditLogService auditLogService,
        ICurrentOrganizationService currentOrganizationService)
    {
        _dbContext = dbContext;
        _bankAdapterRegistry = bankAdapterRegistry;
        _tokenProtectionService = tokenProtectionService;
        _auditLogService = auditLogService;
        _currentOrganizationService = currentOrganizationService;
    }

    public IReadOnlyList<BankAdapterInfoDto> GetAvailableBanks() =>
        _bankAdapterRegistry.GetAvailableAdapters();

    public async Task<IReadOnlyList<BankConnectionListItemDto>> GetAllAsync(bool includeInactive = false)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        var adapters = _bankAdapterRegistry.GetAvailableAdapters()
            .ToDictionary(x => x.BankCode, x => x.DisplayName, StringComparer.OrdinalIgnoreCase);

        var query = _dbContext.BankConnections
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        var items = await query
            .OrderBy(x => x.DisplayName)
            .Select(x => new BankConnectionListItemDto
            {
                Id = x.Id,
                BankCode = x.BankCode,
                DisplayName = x.DisplayName,
                HasAccessToken = x.ProtectedAccessToken != string.Empty,
                HasRefreshToken = x.ProtectedRefreshToken != string.Empty,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        foreach (var item in items)
        {
            item.BankDisplayName = adapters.TryGetValue(item.BankCode, out var displayName)
                ? displayName
                : item.BankCode;
        }

        return items;
    }

    public async Task<UpsertBankConnectionDto?> GetByIdAsync(Guid id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.BankConnections
            .AsNoTracking()
            .Where(x => x.Id == id && x.OrganizationId == organizationId)
            .Select(x => new UpsertBankConnectionDto
            {
                Id = x.Id,
                BankCode = x.BankCode,
                DisplayName = x.DisplayName,
                IsActive = x.IsActive,
                HasAccessToken = x.ProtectedAccessToken != string.Empty,
                HasRefreshToken = x.ProtectedRefreshToken != string.Empty
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(UpsertBankConnectionDto dto, string userId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        Validate(dto, requireTokens: true);

        var adapter = _bankAdapterRegistry.GetRequiredAdapter(dto.BankCode);
        await EnsureDisplayNameUniqueAsync(organizationId, dto.DisplayName);

        var entity = new BankConnection
        {
            OrganizationId = organizationId,
            BankCode = adapter.BankCode,
            DisplayName = dto.DisplayName.Trim(),
            ProtectedAccessToken = _tokenProtectionService.Protect(dto.AccessToken ?? string.Empty),
            ProtectedRefreshToken = _tokenProtectionService.Protect(dto.RefreshToken ?? string.Empty),
            IsActive = dto.IsActive
        };

        _dbContext.BankConnections.Add(entity);
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.BankConnectionCreated,
            userId,
            $"Создано банковское подключение {entity.DisplayName}",
            entity.Id.ToString(),
            nameof(BankConnection));

        return entity.Id;
    }

    public async Task UpdateAsync(UpsertBankConnectionDto dto, string userId)
    {
        if (dto.Id == null || dto.Id == Guid.Empty)
            throw new InvalidOperationException("Идентификатор банковского подключения не указан.");

        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        Validate(dto, requireTokens: false);

        var entity = await _dbContext.BankConnections
            .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.OrganizationId == organizationId);

        if (entity == null)
            throw new InvalidOperationException("Банковское подключение не найдено.");

        var adapter = _bankAdapterRegistry.GetRequiredAdapter(dto.BankCode);
        await EnsureDisplayNameUniqueAsync(organizationId, dto.DisplayName, dto.Id.Value);

        entity.BankCode = adapter.BankCode;
        entity.DisplayName = dto.DisplayName.Trim();
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.AccessToken))
            entity.ProtectedAccessToken = _tokenProtectionService.Protect(dto.AccessToken);

        if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
            entity.ProtectedRefreshToken = _tokenProtectionService.Protect(dto.RefreshToken);

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.BankConnectionUpdated,
            userId,
            $"Обновлено банковское подключение {entity.DisplayName}",
            entity.Id.ToString(),
            nameof(BankConnection));
    }

    public async Task DeactivateAsync(Guid id, string userId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var entity = await _dbContext.BankConnections
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId);

        if (entity == null)
            return;

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.BankConnectionDeactivated,
            userId,
            $"Деактивировано банковское подключение {entity.DisplayName}",
            entity.Id.ToString(),
            nameof(BankConnection));
    }

    private async Task EnsureDisplayNameUniqueAsync(Guid organizationId, string displayName, Guid? excludeId = null)
    {
        var normalized = displayName.Trim();

        var exists = await _dbContext.BankConnections.AnyAsync(x =>
            x.OrganizationId == organizationId &&
            x.DisplayName == normalized &&
            (!excludeId.HasValue || x.Id != excludeId.Value));

        if (exists)
            throw new InvalidOperationException("Банковское подключение с таким названием уже существует.");
    }

    private static void Validate(UpsertBankConnectionDto dto, bool requireTokens)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName))
            throw new InvalidOperationException("Название подключения обязательно.");

        if (requireTokens && string.IsNullOrWhiteSpace(dto.AccessToken))
            throw new InvalidOperationException("Access token обязателен для нового подключения.");

        if (requireTokens && string.IsNullOrWhiteSpace(dto.RefreshToken))
            throw new InvalidOperationException("Refresh token обязателен для нового подключения.");
    }
}
