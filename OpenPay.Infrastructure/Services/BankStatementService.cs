using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.BankStatements;
using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class BankStatementService : IBankStatementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenPayDbContext _dbContext;
    private readonly IBankAdapterRegistry _bankAdapterRegistry;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentOrganizationService _currentOrganizationService;

    public BankStatementService(
        OpenPayDbContext dbContext,
        IBankAdapterRegistry bankAdapterRegistry,
        IAuditLogService auditLogService,
        ICurrentOrganizationService currentOrganizationService)
    {
        _dbContext = dbContext;
        _bankAdapterRegistry = bankAdapterRegistry;
        _auditLogService = auditLogService;
        _currentOrganizationService = currentOrganizationService;
    }

    public async Task<IReadOnlyList<BankStatementListItemDto>> GetAllAsync()
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.BankStatements
            .AsNoTracking()
            .Include(x => x.OrganizationBankAccount)
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BankStatementListItemDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                AccountDisplay = x.OrganizationBankAccount != null
                    ? x.OrganizationBankAccount.BankName + " / " + x.OrganizationBankAccount.AccountNumber
                    : "-",
                PeriodFrom = x.PeriodFrom,
                PeriodTo = x.PeriodTo,
                TotalOperations = x.TotalOperations,
                MatchedOperations = x.MatchedOperations,
                UnmatchedOperations = x.UnmatchedOperations
            })
            .ToListAsync();
    }

    public async Task<BankStatementResultDto> LoadDemoStatementAsync(
        Guid organizationBankAccountId,
        DateOnly periodFrom,
        DateOnly periodTo,
        string userId)
    {
        if (periodFrom > periodTo)
            throw new InvalidOperationException("Дата начала периода не может быть позже даты окончания.");

        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var account = await _dbContext.OrganizationBankAccounts
            .Include(x => x.BankConnection)
            .FirstOrDefaultAsync(x =>
                x.Id == organizationBankAccountId &&
                x.OrganizationId == organizationId);

        if (account == null)
            throw new InvalidOperationException("Счет организации не найден.");

        if (account.BankConnection == null)
            throw new InvalidOperationException("Для счета не настроено банковское подключение.");

        if (!account.BankConnection.IsActive)
            throw new InvalidOperationException("Банковское подключение неактивно.");

        var adapter = _bankAdapterRegistry.GetRequiredAdapter(account.BankConnection.BankCode);
        var operations = (await adapter.LoadStatementAsync(account, account.BankConnection, periodFrom, periodTo)).ToList();

        operations.AddRange(await BuildOperationsFromPaymentsAsync(account.Id, periodFrom, periodTo));

        var statement = new BankStatement
        {
            OrganizationId = organizationId,
            OrganizationBankAccountId = account.Id,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            RawDataJson = JsonSerializer.Serialize(operations, JsonOptions),
            TotalOperations = operations.Count,
            MatchedOperations = 0,
            UnmatchedOperations = operations.Count
        };

        _dbContext.BankStatements.Add(statement);
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.BankStatementImported,
            userId,
            $"Загружена демо-выписка за период {periodFrom:dd.MM.yyyy}-{periodTo:dd.MM.yyyy}",
            statement.Id.ToString(),
            nameof(BankStatement));

        return await ReconcileAsync(statement.Id, userId);
    }

    public async Task<BankStatementResultDto> ReconcileAsync(Guid bankStatementId, string userId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var statement = await _dbContext.BankStatements
            .Include(x => x.OrganizationBankAccount)
            .FirstOrDefaultAsync(x => x.Id == bankStatementId && x.OrganizationId == organizationId);

        if (statement == null)
            throw new InvalidOperationException("Банковская выписка не найдена.");

        var operations = JsonSerializer.Deserialize<List<BankStatementOperationDto>>(statement.RawDataJson, JsonOptions)
            ?? [];

        var items = new List<BankStatementReconciliationItemDto>();

        foreach (var operation in operations)
        {
            var payment = await FindMatchingPaymentAsync(statement.OrganizationBankAccountId, operation);
            var matched = payment != null;

            if (payment != null && payment.Status != PaymentStatus.Executed)
            {
                payment.Status = PaymentStatus.Executed;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.BankResponseMessage = "Платеж сопоставлен с банковской выпиской.";

                if (string.IsNullOrWhiteSpace(payment.BankReferenceId) &&
                    !string.IsNullOrWhiteSpace(operation.BankReferenceId))
                {
                    payment.BankReferenceId = operation.BankReferenceId;
                }
            }

            items.Add(new BankStatementReconciliationItemDto
            {
                OperationId = operation.OperationId,
                OperationDate = operation.OperationDate,
                Amount = operation.Amount,
                Currency = operation.Currency,
                CounterpartyName = operation.CounterpartyName,
                Purpose = operation.Purpose,
                BankReferenceId = operation.BankReferenceId,
                MatchedPaymentOrderId = payment?.Id,
                MatchedDocumentNumber = payment?.DocumentNumber,
                Message = matched
                    ? $"Сопоставлено с платежом {payment!.DocumentNumber}"
                    : "Платеж не найден"
            });
        }

        statement.TotalOperations = operations.Count;
        statement.MatchedOperations = items.Count(x => x.MatchedPaymentOrderId.HasValue);
        statement.UnmatchedOperations = statement.TotalOperations - statement.MatchedOperations;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.BankStatementReconciled,
            userId,
            $"Выполнена сверка выписки: найдено {statement.MatchedOperations}, не найдено {statement.UnmatchedOperations}",
            statement.Id.ToString(),
            nameof(BankStatement));

        return new BankStatementResultDto
        {
            BankStatementId = statement.Id,
            TotalOperations = statement.TotalOperations,
            MatchedOperations = statement.MatchedOperations,
            UnmatchedOperations = statement.UnmatchedOperations,
            Items = items
        };
    }

    private async Task<IReadOnlyList<BankStatementOperationDto>> BuildOperationsFromPaymentsAsync(
        Guid organizationBankAccountId,
        DateOnly periodFrom,
        DateOnly periodTo)
    {
        var from = periodFrom.ToDateTime(TimeOnly.MinValue);
        var toExclusive = periodTo.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return await _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(x => x.Counterparty)
            .Where(x =>
                x.OrganizationBankAccountId == organizationBankAccountId &&
                x.BankReferenceId != null &&
                x.PaymentDate.HasValue &&
                x.PaymentDate.Value >= from &&
                x.PaymentDate.Value < toExclusive)
            .Select(x => new BankStatementOperationDto
            {
                OperationId = "PAY-" + x.Id,
                OperationDate = DateOnly.FromDateTime(x.PaymentDate!.Value),
                Amount = x.Amount,
                Currency = x.Currency,
                CounterpartyName = x.Counterparty != null ? x.Counterparty.FullName : "-",
                CounterpartyAccountNumber = x.Counterparty != null ? x.Counterparty.AccountNumber : string.Empty,
                Purpose = x.Purpose,
                BankReferenceId = x.BankReferenceId
            })
            .ToListAsync();
    }

    private async Task<PaymentOrder?> FindMatchingPaymentAsync(
        Guid organizationBankAccountId,
        BankStatementOperationDto operation)
    {
        if (!string.IsNullOrWhiteSpace(operation.BankReferenceId))
        {
            var byReference = await _dbContext.PaymentOrders
                .FirstOrDefaultAsync(x =>
                    x.OrganizationBankAccountId == organizationBankAccountId &&
                    x.BankReferenceId == operation.BankReferenceId);

            if (byReference != null)
                return byReference;
        }

        var operationDate = operation.OperationDate.ToDateTime(TimeOnly.MinValue);
        var toExclusive = operationDate.AddDays(1);

        return await _dbContext.PaymentOrders
            .FirstOrDefaultAsync(x =>
                x.OrganizationBankAccountId == organizationBankAccountId &&
                x.Amount == operation.Amount &&
                x.Currency == operation.Currency &&
                x.PaymentDate.HasValue &&
                x.PaymentDate.Value >= operationDate &&
                x.PaymentDate.Value < toExclusive &&
                x.Purpose == operation.Purpose);
    }
}
