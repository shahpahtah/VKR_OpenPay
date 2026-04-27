using System.Globalization;
using System.Text;
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
            .Include(x => x.ApprovalRoute)
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.DocumentNumber.Contains(search) ||
                x.Purpose.Contains(search) ||
                x.ExpenseType.Contains(search) ||
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
                ExpenseType = x.ExpenseType,
                Status = x.Status.ToString(),
                ApprovalRouteName = x.ApprovalRoute != null ? x.ApprovalRoute.Name : null
            })
            .ToListAsync();
    }

    public async Task<UpsertPaymentOrderDto?> GetByIdAsync(Guid id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(x => x.ApprovalRoute)
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
                ExpenseType = x.ExpenseType,
                Purpose = x.Purpose,
                CurrentStatus = x.Status,
                BankReferenceId = x.BankReferenceId,
                BankResponseMessage = x.BankResponseMessage,
                SignedAt = x.SignedAt,
                SignatureReference = x.SignatureReference,
                SentAt = x.SentAt,
                ProcessedAt = x.ProcessedAt,
                ApprovalRouteId = x.ApprovalRouteId,
                ApprovalRouteName = x.ApprovalRoute != null ? x.ApprovalRoute.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(UpsertPaymentOrderDto dto, string createdByUserId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        var references = await ValidateReferencesAsync(dto, organizationId);
        await EnsureNoDuplicateAsync(dto, organizationId);

        var route = await ResolveApprovalRouteAsync(organizationId, dto, references.Account.ResponsibleUnit);

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
            ExpenseType = Normalize(dto.ExpenseType) ?? "Прочее",
            Purpose = dto.Purpose.Trim(),
            Status = PaymentStatus.Draft,
            ApprovalRouteId = route?.Id,
            CreatedByUserId = createdByUserId
        };

        _dbContext.PaymentOrders.Add(entity);
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.PaymentCreated,
            createdByUserId,
            route == null
                ? $"Создано платежное поручение {entity.DocumentNumber}"
                : $"Создано платежное поручение {entity.DocumentNumber}; назначен маршрут {route.Name}",
            entity.Id.ToString(),
            nameof(PaymentOrder));

        return entity.Id;
    }

    public async Task UpdateAsync(UpsertPaymentOrderDto dto, string updatedByUserId)
    {
        if (dto.Id == null || dto.Id == Guid.Empty)
            throw new InvalidOperationException("Идентификатор платежа не указан.");

        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        var references = await ValidateReferencesAsync(dto, organizationId);
        await EnsureNoDuplicateAsync(dto, organizationId, dto.Id.Value);

        var entity = await _dbContext.PaymentOrders
            .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.OrganizationId == organizationId);

        if (entity == null)
            throw new InvalidOperationException("Платежное поручение не найдено.");

        if (entity.Status != PaymentStatus.Draft && entity.Status != PaymentStatus.Rework)
            throw new InvalidOperationException("Редактировать можно только платежи в статусе Draft или Rework.");

        var route = await ResolveApprovalRouteAsync(organizationId, dto, references.Account.ResponsibleUnit);

        entity.DocumentNumber = string.IsNullOrWhiteSpace(dto.DocumentNumber)
            ? entity.DocumentNumber
            : dto.DocumentNumber.Trim();
        entity.PaymentDate = dto.PaymentDate;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.OrganizationBankAccountId = dto.OrganizationBankAccountId;
        entity.Amount = dto.Amount;
        entity.Currency = dto.Currency.Trim().ToUpperInvariant();
        entity.ExpenseType = Normalize(dto.ExpenseType) ?? "Прочее";
        entity.Purpose = dto.Purpose.Trim();
        entity.ApprovalRouteId = route?.Id;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.PaymentUpdated,
            updatedByUserId,
            route == null
                ? $"Обновлено платежное поручение {entity.DocumentNumber}"
                : $"Обновлено платежное поручение {entity.DocumentNumber}; назначен маршрут {route.Name}",
            entity.Id.ToString(),
            nameof(PaymentOrder));
    }

    public async Task<PaymentOrderImportResultDto> ImportFromCsvAsync(Stream csvStream, string createdByUserId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var result = new PaymentOrderImportResultDto();

        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("CSV-файл пустой.");

        var separator = DetectSeparator(headerLine);
        var headers = SplitCsvLine(headerLine, separator);

        var documentNumberIndex = FindColumnIndex(headers, "DocumentNumber", "Номер");
        var paymentDateIndex = FindColumnIndex(headers, "PaymentDate", "ДатаПлатежа", "Дата");
        var counterpartyInnIndex = FindColumnIndex(headers, "CounterpartyInn", "INN", "Inn", "КонтрагентИНН");
        var accountNumberIndex = FindColumnIndex(headers, "OrganizationAccountNumber", "AccountNumber", "СчетОрганизации");
        var amountIndex = FindColumnIndex(headers, "Amount", "Сумма");
        var currencyIndex = FindColumnIndex(headers, "Currency", "Валюта");
        var expenseTypeIndex = FindColumnIndex(headers, "ExpenseType", "ТипРасхода", "Статья");
        var purposeIndex = FindColumnIndex(headers, "Purpose", "Назначение");

        if (paymentDateIndex < 0 || counterpartyInnIndex < 0 || accountNumberIndex < 0 ||
            amountIndex < 0 || currencyIndex < 0 || purposeIndex < 0)
        {
            throw new InvalidOperationException(
                "CSV должен содержать колонки: PaymentDate, CounterpartyInn, OrganizationAccountNumber, Amount, Currency, Purpose. Колонки DocumentNumber и ExpenseType необязательны.");
        }

        var counterparties = await _dbContext.Counterparties
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IsActive)
            .ToDictionaryAsync(x => x.Inn, x => x.Id);

        var accounts = await _dbContext.OrganizationBankAccounts
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IsActive)
            .ToDictionaryAsync(x => x.AccountNumber, x => new { x.Id, x.Currency });

        var rowNumber = 1;

        while (await reader.ReadLineAsync() is { } line)
        {
            rowNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = SplitCsvLine(line, separator);
            string GetValue(int index) => index >= 0 && index < values.Count ? values[index].Trim() : string.Empty;

            var documentNumber = documentNumberIndex >= 0 ? GetValue(documentNumberIndex) : string.Empty;
            var paymentDateText = GetValue(paymentDateIndex);
            var counterpartyInn = GetValue(counterpartyInnIndex);
            var accountNumber = GetValue(accountNumberIndex);
            var amountText = GetValue(amountIndex);
            var currency = GetValue(currencyIndex);
            var expenseType = expenseTypeIndex >= 0 ? GetValue(expenseTypeIndex) : string.Empty;
            var purpose = GetValue(purposeIndex);

            result.TotalRows++;

            var errors = new List<string>();

            if (!TryParseDate(paymentDateText, out var paymentDate))
                errors.Add("Некорректная дата платежа.");

            Guid counterpartyId = Guid.Empty;
            Guid organizationAccountId = Guid.Empty;

            if (string.IsNullOrWhiteSpace(counterpartyInn))
            {
                errors.Add("Не заполнен CounterpartyInn.");
            }
            else if (!counterparties.TryGetValue(counterpartyInn, out counterpartyId))
            {
                errors.Add("Контрагент с таким INN не найден среди активных контрагентов организации.");
            }

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                errors.Add("Не заполнен OrganizationAccountNumber.");
            }
            else if (!accounts.TryGetValue(accountNumber, out var account))
            {
                errors.Add("Счет организации не найден среди активных счетов.");
            }
            else
            {
                organizationAccountId = account.Id;

                if (!string.Equals(account.Currency, currency, StringComparison.OrdinalIgnoreCase))
                    errors.Add("Валюта платежа не совпадает с валютой счета организации.");
            }

            if (!TryParseAmount(amountText, out var amount))
                errors.Add("Некорректная сумма.");

            if (string.IsNullOrWhiteSpace(currency))
                errors.Add("Не заполнена валюта.");

            if (string.IsNullOrWhiteSpace(purpose))
                errors.Add("Не заполнено назначение платежа.");

            var dto = new UpsertPaymentOrderDto
            {
                DocumentNumber = documentNumber,
                PaymentDate = paymentDate,
                CounterpartyId = counterpartyId,
                OrganizationBankAccountId = organizationAccountId,
                Amount = amount,
                Currency = currency,
                ExpenseType = expenseType,
                Purpose = purpose
            };

            if (errors.Count == 0)
            {
                try
                {
                    await EnsureNoDuplicateAsync(dto, organizationId);
                }
                catch (InvalidOperationException ex)
                {
                    errors.Add(ex.Message);
                }
            }

            if (errors.Count > 0)
            {
                result.ErrorRows++;
                result.Items.Add(new PaymentOrderImportRowResultDto
                {
                    RowNumber = rowNumber,
                    DocumentNumber = documentNumber,
                    CounterpartyInn = counterpartyInn,
                    AmountText = amountText,
                    IsImported = false,
                    Message = string.Join(" ", errors)
                });

                continue;
            }

            try
            {
                await CreateAsync(dto, createdByUserId);

                result.ImportedRows++;
                result.Items.Add(new PaymentOrderImportRowResultDto
                {
                    RowNumber = rowNumber,
                    DocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? "(автогенерация)" : documentNumber,
                    CounterpartyInn = counterpartyInn,
                    AmountText = amountText,
                    IsImported = true,
                    Message = "Импортировано успешно."
                });
            }
            catch (InvalidOperationException ex)
            {
                result.ErrorRows++;
                result.Items.Add(new PaymentOrderImportRowResultDto
                {
                    RowNumber = rowNumber,
                    DocumentNumber = documentNumber,
                    CounterpartyInn = counterpartyInn,
                    AmountText = amountText,
                    IsImported = false,
                    Message = ex.Message
                });
            }
        }

        return result;
    }

    private async Task<(Counterparty Counterparty, OrganizationBankAccount Account)> ValidateReferencesAsync(
        UpsertPaymentOrderDto dto,
        Guid organizationId)
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

        if (!string.Equals(account.Currency, dto.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Валюта платежа не совпадает с валютой счета организации.");

        return (counterparty, account);
    }

    private async Task EnsureNoDuplicateAsync(
        UpsertPaymentOrderDto dto,
        Guid organizationId,
        Guid? excludePaymentId = null)
    {
        if (!dto.PaymentDate.HasValue)
            return;

        var fromDate = dto.PaymentDate.Value.Date.AddDays(-7);
        var toDateExclusive = dto.PaymentDate.Value.Date.AddDays(8);

        var exists = await _dbContext.PaymentOrders
            .AsNoTracking()
            .AnyAsync(x =>
                x.OrganizationId == organizationId &&
                (!excludePaymentId.HasValue || x.Id != excludePaymentId.Value) &&
                x.CounterpartyId == dto.CounterpartyId &&
                x.OrganizationBankAccountId == dto.OrganizationBankAccountId &&
                x.Amount == dto.Amount &&
                x.Purpose == dto.Purpose.Trim() &&
                x.PaymentDate.HasValue &&
                x.PaymentDate.Value >= fromDate &&
                x.PaymentDate.Value < toDateExclusive);

        if (exists)
            throw new InvalidOperationException("Обнаружен возможный дубликат платежа за период ±7 дней.");
    }

    private async Task<ApprovalRoute?> ResolveApprovalRouteAsync(
        Guid organizationId,
        UpsertPaymentOrderDto dto,
        string department)
    {
        var expenseType = Normalize(dto.ExpenseType);
        var normalizedDepartment = Normalize(department);

        var routes = await _dbContext.ApprovalRoutes
            .AsNoTracking()
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.IsActive &&
                (!x.MinAmount.HasValue || dto.Amount >= x.MinAmount.Value) &&
                (!x.MaxAmount.HasValue || dto.Amount <= x.MaxAmount.Value))
            .ToListAsync();

        return routes
            .Where(x => x.ExpenseType == null || string.Equals(x.ExpenseType, expenseType, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Department == null || string.Equals(x.Department, normalizedDepartment, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.ExpenseType != null ? 1 : 0)
            .ThenByDescending(x => x.Department != null ? 1 : 0)
            .ThenByDescending(x => x.MinAmount ?? 0)
            .FirstOrDefault();
    }

    private async Task<string> GenerateDocumentNumberAsync(Guid organizationId)
    {
        var prefix = $"PAY-{DateTime.UtcNow:yyyyMM}-";

        var count = await _dbContext.PaymentOrders.CountAsync(x =>
            x.OrganizationId == organizationId &&
            x.DocumentNumber.StartsWith(prefix));

        return prefix + (count + 1).ToString("D4");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static char DetectSeparator(string headerLine)
    {
        var semicolonCount = headerLine.Count(x => x == ';');
        var commaCount = headerLine.Count(x => x == ',');

        return semicolonCount >= commaCount ? ';' : ',';
    }

    private static List<string> SplitCsvLine(string line, char separator)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == separator && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }

    private static int FindColumnIndex(List<string> headers, params string[] names)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim();

            if (names.Any(name => string.Equals(header, name, StringComparison.OrdinalIgnoreCase)))
                return i;
        }

        return -1;
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd" };

        return DateTime.TryParseExact(
                   value,
                   formats,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out date)
               || DateTime.TryParse(value, new CultureInfo("ru-RU"), DateTimeStyles.None, out date);
    }

    private static bool TryParseAmount(string value, out decimal amount)
    {
        value = value.Replace(" ", "");

        return decimal.TryParse(value, NumberStyles.Number, new CultureInfo("ru-RU"), out amount)
               || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}
