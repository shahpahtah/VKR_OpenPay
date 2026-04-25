using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Payments;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class PaymentOrderService : IPaymentOrderService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentOrganizationService _currentOrganizationService;

    public PaymentOrderService(
        OpenPayDbContext dbContext,
        IAuditLogService auditLogService,
        ICurrentOrganizationService currentOrganizationService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _currentOrganizationService = currentOrganizationService;
    }

    public async Task<IReadOnlyList<PaymentOrderListItemDto>> GetAllAsync(string? search = null)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var query = _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(x => x.Counterparty)
            .Include(x => x.OrganizationBankAccount)
            .Where(x => x.OrganizationId == organizationId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.DocumentNumber.Contains(search) ||
                x.Purpose.Contains(search) ||
                (x.Counterparty != null && x.Counterparty.FullName.Contains(search)));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PaymentOrderListItemDto
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                CreatedAt = x.CreatedAt,
                PaymentDate = x.PaymentDate,
                CounterpartyName = x.Counterparty != null ? x.Counterparty.FullName : "-",
                OrganizationAccountDisplay = x.OrganizationBankAccount != null
                    ? x.OrganizationBankAccount.BankName + " / " + x.OrganizationBankAccount.AccountNumber
                    : "-",
                Amount = x.Amount,
                Currency = x.Currency,
                Status = x.Status.ToString()
            })
            .ToListAsync();
    }

    public async Task<UpsertPaymentOrderDto?> GetByIdAsync(Guid id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.PaymentOrders
            .AsNoTracking()
            .Where(x => x.Id == id && x.OrganizationId == organizationId)
            .Select(x => new UpsertPaymentOrderDto
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                PaymentDate = x.PaymentDate,
                CounterpartyId = x.CounterpartyId,
                OrganizationBankAccountId = x.OrganizationBankAccountId,
                Amount = x.Amount,
                Currency = x.Currency,
                Purpose = x.Purpose,
                CurrentStatus = x.Status,
                BankReferenceId = x.BankReferenceId,
                BankResponseMessage = x.BankResponseMessage,
                SentAt = x.SentAt,
                ProcessedAt = x.ProcessedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(UpsertPaymentOrderDto dto, string createdByUserId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        await ValidateReferencesAsync(dto, organizationId);

        var entity = new PaymentOrder
        {
            OrganizationId = organizationId,
            DocumentNumber = string.IsNullOrWhiteSpace(dto.DocumentNumber)
                ? await GenerateDocumentNumberAsync(organizationId)
                : dto.DocumentNumber.Trim(),
            CreatedAt = DateTime.UtcNow,
            PaymentDate = dto.PaymentDate,
            CounterpartyId = dto.CounterpartyId,
            OrganizationBankAccountId = dto.OrganizationBankAccountId,
            Amount = dto.Amount,
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            Purpose = dto.Purpose.Trim(),
            Status = PaymentStatus.Draft,
            CreatedByUserId = createdByUserId
        };

        _dbContext.PaymentOrders.Add(entity);
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.PaymentCreated,
            createdByUserId,
            $"Создано платежное поручение {entity.DocumentNumber}",
            entity.Id.ToString(),
            nameof(PaymentOrder));

        return entity.Id;
    }

    public async Task UpdateAsync(UpsertPaymentOrderDto dto, string updatedByUserId)
    {
        if (dto.Id == null || dto.Id == Guid.Empty)
            throw new InvalidOperationException("Идентификатор платежа не указан.");

        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        await ValidateReferencesAsync(dto, organizationId);

        var entity = await _dbContext.PaymentOrders
            .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.OrganizationId == organizationId);

        if (entity == null)
            throw new InvalidOperationException("Платежное поручение не найдено.");

        if (entity.Status != PaymentStatus.Draft && entity.Status != PaymentStatus.Rework)
            throw new InvalidOperationException("Редактировать можно только платежи в статусе Draft или Rework.");

        entity.DocumentNumber = string.IsNullOrWhiteSpace(dto.DocumentNumber)
            ? entity.DocumentNumber
            : dto.DocumentNumber.Trim();
        entity.PaymentDate = dto.PaymentDate;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.OrganizationBankAccountId = dto.OrganizationBankAccountId;
        entity.Amount = dto.Amount;
        entity.Currency = dto.Currency.Trim().ToUpperInvariant();
        entity.Purpose = dto.Purpose.Trim();

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.PaymentUpdated,
            updatedByUserId,
            $"Обновлено платежное поручение {entity.DocumentNumber}",
            entity.Id.ToString(),
            nameof(PaymentOrder));
    }

    private async Task ValidateReferencesAsync(UpsertPaymentOrderDto dto, Guid organizationId)
    {
        var counterparty = await _dbContext.Counterparties
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.CounterpartyId && x.OrganizationId == organizationId);

        if (counterparty == null)
            throw new InvalidOperationException("Выбранный контрагент не найден.");

        if (!counterparty.IsActive)
            throw new InvalidOperationException("Выбранный контрагент деактивирован.");

        var account = await _dbContext.OrganizationBankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.OrganizationBankAccountId && x.OrganizationId == organizationId);

        if (account == null)
            throw new InvalidOperationException("Выбранный счет организации не найден.");

        if (!account.IsActive)
            throw new InvalidOperationException("Выбранный счет организации деактивирован.");
    }

    private async Task<string> GenerateDocumentNumberAsync(Guid organizationId)
    {
        var prefix = $"PAY-{DateTime.UtcNow:yyyyMM}-";

        var count = await _dbContext.PaymentOrders.CountAsync(x =>
            x.OrganizationId == organizationId &&
            x.DocumentNumber.StartsWith(prefix));

        return prefix + (count + 1).ToString("D4");
    }
}