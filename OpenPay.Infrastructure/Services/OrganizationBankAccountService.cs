using Microsoft.EntityFrameworkCore;
using OpenPay.Application.Common;
using OpenPay.Application.DTOs.Accounts;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class OrganizationBankAccountService : IOrganizationBankAccountService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly ICurrentOrganizationService _currentOrganizationService;
    private readonly IAuditLogService _auditLogService;

    public OrganizationBankAccountService(
        OpenPayDbContext dbContext,
        ICurrentOrganizationService currentOrganizationService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentOrganizationService = currentOrganizationService;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<OrganizationBankAccountListItemDto>> GetAllAsync(string? search = null, bool? isActive = null)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var query = _dbContext.OrganizationBankAccounts
            .AsNoTracking()
            .Include(x => x.BankConnection)
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.BankName.Contains(search) ||
                x.Bic.Contains(search) ||
                x.AccountNumber.Contains(search) ||
                x.ResponsibleUnit.Contains(search) ||
                (x.BankConnection != null && x.BankConnection.DisplayName.Contains(search)));
        }

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        return await query
            .OrderBy(x => x.BankName)
            .ThenBy(x => x.AccountNumber)
            .Select(x => new OrganizationBankAccountListItemDto
            {
                Id = x.Id,
                Bic = x.Bic,
                AccountNumber = x.AccountNumber,
                BankName = x.BankName,
                Currency = x.Currency,
                ResponsibleUnit = x.ResponsibleUnit,
                BankConnectionDisplay = x.BankConnection != null ? x.BankConnection.DisplayName : "Не настроено",
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<UpsertOrganizationBankAccountDto?> GetByIdAsync(Guid id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.OrganizationBankAccounts
            .AsNoTracking()
            .Where(x => x.Id == id && x.OrganizationId == organizationId)
            .Select(x => new UpsertOrganizationBankAccountDto
            {
                Id = x.Id,
                Bic = x.Bic,
                AccountNumber = x.AccountNumber,
                BankName = x.BankName,
                Currency = x.Currency,
                ResponsibleUnit = x.ResponsibleUnit,
                BankConnectionId = x.BankConnectionId,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(UpsertOrganizationBankAccountDto dto)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        await ValidateAsync(dto, organizationId);
        await ValidateUniquenessAsync(dto, organizationId);

        var entity = new OrganizationBankAccount
        {
            OrganizationId = organizationId,
            Bic = dto.Bic.Trim(),
            AccountNumber = dto.AccountNumber.Trim(),
            BankName = dto.BankName.Trim(),
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            ResponsibleUnit = dto.ResponsibleUnit.Trim(),
            BankConnectionId = dto.BankConnectionId,
            IsActive = dto.IsActive
        };

        _dbContext.OrganizationBankAccounts.Add(entity);
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.BankAccountCreated,
            null,
            $"Создан банковский счет {entity.BankName} / {entity.AccountNumber}",
            entity.Id.ToString(),
            nameof(OrganizationBankAccount));

        return entity.Id;
    }

    public async Task UpdateAsync(UpsertOrganizationBankAccountDto dto)
    {
        if (dto.Id == null || dto.Id == Guid.Empty)
            throw new InvalidOperationException("Идентификатор счета не указан.");

        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        await ValidateAsync(dto, organizationId);
        await ValidateUniquenessAsync(dto, organizationId);

        var entity = await _dbContext.OrganizationBankAccounts
            .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.OrganizationId == organizationId);

        if (entity == null)
            throw new InvalidOperationException("Банковский счет не найден.");

        entity.Bic = dto.Bic.Trim();
        entity.AccountNumber = dto.AccountNumber.Trim();
        entity.BankName = dto.BankName.Trim();
        entity.Currency = dto.Currency.Trim().ToUpperInvariant();
        entity.ResponsibleUnit = dto.ResponsibleUnit.Trim();
        entity.BankConnectionId = dto.BankConnectionId;
        entity.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.BankAccountUpdated,
            null,
            $"Обновлен банковский счет {entity.BankName} / {entity.AccountNumber}",
            entity.Id.ToString(),
            nameof(OrganizationBankAccount));
    }

    public async Task DeactivateAsync(Guid id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var entity = await _dbContext.OrganizationBankAccounts
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId);

        if (entity == null)
            return;

        entity.IsActive = false;
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.BankAccountDeactivated,
            null,
            $"Деактивирован банковский счет {entity.BankName} / {entity.AccountNumber}",
            entity.Id.ToString(),
            nameof(OrganizationBankAccount));
    }

    private async Task ValidateAsync(UpsertOrganizationBankAccountDto dto, Guid organizationId)
    {
        var errors = new List<string>();

        if (!BankingValidators.IsValidBic(dto.Bic))
            errors.Add(BankingValidators.GetBicError(dto.Bic)!);

        if (!BankingValidators.IsValidSettlementAccount(dto.Bic, dto.AccountNumber))
            errors.Add(BankingValidators.GetSettlementAccountError(dto.Bic, dto.AccountNumber)!);

        if (!string.IsNullOrWhiteSpace(dto.Currency) && dto.Currency.Trim().Length != 3)
            errors.Add("Код валюты должен содержать 3 символа.");

        if (dto.BankConnectionId.HasValue)
        {
            var connectionExists = await _dbContext.BankConnections.AnyAsync(x =>
                x.Id == dto.BankConnectionId.Value &&
                x.OrganizationId == organizationId &&
                x.IsActive);

            if (!connectionExists)
                errors.Add("Выбранное банковское подключение не найдено или неактивно.");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));
    }

    private async Task ValidateUniquenessAsync(UpsertOrganizationBankAccountDto dto, Guid organizationId)
    {
        var currentId = dto.Id ?? Guid.Empty;
        var bic = dto.Bic.Trim();
        var accountNumber = dto.AccountNumber.Trim();

        var exists = await _dbContext.OrganizationBankAccounts.AnyAsync(x =>
            x.OrganizationId == organizationId &&
            x.Id != currentId &&
            x.Bic == bic &&
            x.AccountNumber == accountNumber);

        if (exists)
            throw new InvalidOperationException("Счет с таким БИК и номером уже существует.");
    }
}
