using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Accounts;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class OrganizationBankAccountService : IOrganizationBankAccountService
{
    private readonly OpenPayDbContext _dbContext;

    public OrganizationBankAccountService(OpenPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OrganizationBankAccountListItemDto>> GetAllAsync(string? search = null, bool? isActive = null)
    {
        var query = _dbContext.OrganizationBankAccounts
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.BankName.Contains(search) ||
                x.Bic.Contains(search) ||
                x.AccountNumber.Contains(search) ||
                x.ResponsibleUnit.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

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
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<UpsertOrganizationBankAccountDto?> GetByIdAsync(Guid id)
    {
        return await _dbContext.OrganizationBankAccounts
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UpsertOrganizationBankAccountDto
            {
                Id = x.Id,
                Bic = x.Bic,
                AccountNumber = x.AccountNumber,
                BankName = x.BankName,
                Currency = x.Currency,
                ResponsibleUnit = x.ResponsibleUnit,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(UpsertOrganizationBankAccountDto dto)
    {
        await ValidateUniquenessAsync(dto);

        var entity = new OrganizationBankAccount
        {
            Bic = dto.Bic.Trim(),
            AccountNumber = dto.AccountNumber.Trim(),
            BankName = dto.BankName.Trim(),
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            ResponsibleUnit = dto.ResponsibleUnit.Trim(),
            IsActive = dto.IsActive
        };

        _dbContext.OrganizationBankAccounts.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity.Id;
    }

    public async Task UpdateAsync(UpsertOrganizationBankAccountDto dto)
    {
        if (dto.Id == null || dto.Id == Guid.Empty)
            throw new InvalidOperationException("Идентификатор счета не указан.");

        await ValidateUniquenessAsync(dto);

        var entity = await _dbContext.OrganizationBankAccounts.FirstOrDefaultAsync(x => x.Id == dto.Id.Value);
        if (entity == null)
            throw new InvalidOperationException("Банковский счет не найден.");

        entity.Bic = dto.Bic.Trim();
        entity.AccountNumber = dto.AccountNumber.Trim();
        entity.BankName = dto.BankName.Trim();
        entity.Currency = dto.Currency.Trim().ToUpperInvariant();
        entity.ResponsibleUnit = dto.ResponsibleUnit.Trim();
        entity.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeactivateAsync(Guid id)
    {
        var entity = await _dbContext.OrganizationBankAccounts.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return;

        entity.IsActive = false;
        await _dbContext.SaveChangesAsync();
    }

    private async Task ValidateUniquenessAsync(UpsertOrganizationBankAccountDto dto)
    {
        var currentId = dto.Id ?? Guid.Empty;
        var bic = dto.Bic.Trim();
        var accountNumber = dto.AccountNumber.Trim();

        var exists = await _dbContext.OrganizationBankAccounts.AnyAsync(x =>
            x.Id != currentId &&
            x.Bic == bic &&
            x.AccountNumber == accountNumber);

        if (exists)
            throw new InvalidOperationException("Счет с таким БИК и номером уже существует.");
    }
}