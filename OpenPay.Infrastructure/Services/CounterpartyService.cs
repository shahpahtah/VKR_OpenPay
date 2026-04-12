using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Counterparties;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class CounterpartyService : ICounterpartyService
{
    private readonly OpenPayDbContext _dbContext;

    public CounterpartyService(OpenPayDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CounterpartyListItemDto>> GetAllAsync(string? search = null, bool? isActive = null)
    {
        var query = _dbContext.Counterparties.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Inn.Contains(search) ||
                x.FullName.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(x => x.FullName)
            .Select(x => new CounterpartyListItemDto
            {
                Id = x.Id,
                Inn = x.Inn,
                Kpp = x.Kpp,
                FullName = x.FullName,
                Bic = x.Bic,
                AccountNumber = x.AccountNumber,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<UpsertCounterpartyDto?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Counterparties
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UpsertCounterpartyDto
            {
                Id = x.Id,
                Inn = x.Inn,
                Kpp = x.Kpp,
                FullName = x.FullName,
                Bic = x.Bic,
                AccountNumber = x.AccountNumber,
                CorrespondentAccount = x.CorrespondentAccount,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(UpsertCounterpartyDto dto)
    {
        await ValidateUniquenessAsync(dto);

        var entity = new Counterparty
        {
            Inn = dto.Inn.Trim(),
            Kpp = dto.Kpp.Trim(),
            FullName = dto.FullName.Trim(),
            Bic = dto.Bic.Trim(),
            AccountNumber = dto.AccountNumber.Trim(),
            CorrespondentAccount = dto.CorrespondentAccount.Trim(),
            IsActive = dto.IsActive
        };

        _dbContext.Counterparties.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity.Id;
    }

    public async Task UpdateAsync(UpsertCounterpartyDto dto)
    {
        if (dto.Id == null || dto.Id == Guid.Empty)
            throw new InvalidOperationException("Идентификатор контрагента не указан.");

        await ValidateUniquenessAsync(dto);

        var entity = await _dbContext.Counterparties.FirstOrDefaultAsync(x => x.Id == dto.Id.Value);
        if (entity == null)
            throw new InvalidOperationException("Контрагент не найден.");

        entity.Inn = dto.Inn.Trim();
        entity.Kpp = dto.Kpp.Trim();
        entity.FullName = dto.FullName.Trim();
        entity.Bic = dto.Bic.Trim();
        entity.AccountNumber = dto.AccountNumber.Trim();
        entity.CorrespondentAccount = dto.CorrespondentAccount.Trim();
        entity.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeactivateAsync(Guid id)
    {
        var entity = await _dbContext.Counterparties.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return;

        entity.IsActive = false;
        await _dbContext.SaveChangesAsync();
    }

    private async Task ValidateUniquenessAsync(UpsertCounterpartyDto dto)
    {
        var normalizedInn = dto.Inn.Trim();
        var normalizedAccount = dto.AccountNumber.Trim();
        var currentId = dto.Id ?? Guid.Empty;

        var exists = await _dbContext.Counterparties.AnyAsync(x =>
            x.Id != currentId &&
            x.Inn == normalizedInn &&
            x.AccountNumber == normalizedAccount);

        if (exists)
            throw new InvalidOperationException("Контрагент с таким ИНН и расчетным счетом уже существует.");
    }
}