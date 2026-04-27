using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.Common;
using OpenPay.Application.DTOs.Counterparties;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class CounterpartyService : ICounterpartyService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly ICurrentOrganizationService _currentOrganizationService;
    private readonly IAuditLogService _auditLogService;

    public CounterpartyService(
        OpenPayDbContext dbContext,
        ICurrentOrganizationService currentOrganizationService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentOrganizationService = currentOrganizationService;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<CounterpartyListItemDto>> GetAllAsync(string? search = null, bool? isActive = null)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var query = _dbContext.Counterparties
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Inn.Contains(search) ||
                x.FullName.Contains(search) ||
                x.AccountNumber.Contains(search));
        }

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

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
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.Counterparties
            .AsNoTracking()
            .Where(x => x.Id == id && x.OrganizationId == organizationId)
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
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        ValidateBankDetails(dto);
        await EnsureInnUniqueAsync(organizationId, dto.Inn);
        await ValidateUniquenessAsync(dto, organizationId);

        var entity = new Counterparty
        {
            OrganizationId = organizationId,
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

        await _auditLogService.LogAsync(
            AuditEventType.CounterpartyCreated,
            null,
            $"Создан контрагент {entity.FullName}",
            entity.Id.ToString(),
            nameof(Counterparty));

        return entity.Id;
    }

    public async Task UpdateAsync(UpsertCounterpartyDto dto)
    {
        if (dto.Id == null || dto.Id == Guid.Empty)
            throw new InvalidOperationException("Идентификатор контрагента не указан.");

        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();
        ValidateBankDetails(dto);
        await EnsureInnUniqueAsync(organizationId, dto.Inn, dto.Id);
        await ValidateUniquenessAsync(dto, organizationId);

        var entity = await _dbContext.Counterparties
            .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.OrganizationId == organizationId);

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

        await _auditLogService.LogAsync(
            AuditEventType.CounterpartyUpdated,
            null,
            $"Обновлен контрагент {entity.FullName}",
            entity.Id.ToString(),
            nameof(Counterparty));
    }

    public async Task DeactivateAsync(Guid id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var entity = await _dbContext.Counterparties
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId);

        if (entity == null)
            return;

        entity.IsActive = false;
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.CounterpartyDeactivated,
            null,
            $"Деактивирован контрагент {entity.FullName}",
            entity.Id.ToString(),
            nameof(Counterparty));
    }

    public async Task<CounterpartyImportResultDto> ImportFromCsvAsync(Stream csvStream)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var result = new CounterpartyImportResultDto();

        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("CSV-файл пустой.");

        var separator = DetectSeparator(headerLine);
        var headers = SplitCsvLine(headerLine, separator);

        var innIndex = FindColumnIndex(headers, "INN", "Inn", "ИНН");
        var nameIndex = FindColumnIndex(headers, "Name", "FullName", "Наименование");
        var kppIndex = FindColumnIndex(headers, "KPP", "Kpp", "КПП");
        var bicIndex = FindColumnIndex(headers, "BIC", "Bic", "БИК");
        var accountIndex = FindColumnIndex(headers, "AccountNumber", "Счет", "РасчетныйСчет");
        var corrIndex = FindColumnIndex(headers, "CorrespondentAccount", "CorrAccount", "КоррСчет");

        if (innIndex < 0 || nameIndex < 0 || kppIndex < 0 || bicIndex < 0 || accountIndex < 0 || corrIndex < 0)
            throw new InvalidOperationException(
                "CSV должен содержать колонки: INN, Name, KPP, BIC, AccountNumber, CorrespondentAccount.");

        var existingInns = await _dbContext.Counterparties
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => x.Inn)
            .ToListAsync();

        var existingInnSet = new HashSet<string>(existingInns, StringComparer.OrdinalIgnoreCase);
        var importBatchInnSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entitiesToAdd = new List<Counterparty>();
        var rowNumber = 1;

        while (await reader.ReadLineAsync() is { } line)
        {
            rowNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = SplitCsvLine(line, separator);
            string GetValue(int index) => index >= 0 && index < values.Count ? values[index].Trim() : string.Empty;

            var inn = GetValue(innIndex);
            var fullName = GetValue(nameIndex);
            var kpp = GetValue(kppIndex);
            var bic = GetValue(bicIndex);
            var accountNumber = GetValue(accountIndex);
            var correspondentAccount = GetValue(corrIndex);

            result.TotalRows++;

            var errors = ValidateCounterpartyImportRow(
                inn,
                fullName,
                kpp,
                bic,
                accountNumber,
                correspondentAccount);

            if (!string.IsNullOrWhiteSpace(inn) && existingInnSet.Contains(inn))
                errors.Add("Контрагент с таким INN уже существует.");

            if (!string.IsNullOrWhiteSpace(inn) && importBatchInnSet.Contains(inn))
                errors.Add("Дублирующийся INN внутри файла.");

            if (errors.Count > 0)
            {
                result.ErrorRows++;
                result.Items.Add(new CounterpartyImportRowResultDto
                {
                    RowNumber = rowNumber,
                    Inn = inn,
                    FullName = fullName,
                    IsImported = false,
                    Message = string.Join(" ", errors)
                });

                continue;
            }

            importBatchInnSet.Add(inn);

            entitiesToAdd.Add(new Counterparty
            {
                OrganizationId = organizationId,
                Inn = inn,
                Kpp = kpp,
                FullName = fullName,
                Bic = bic,
                AccountNumber = accountNumber,
                CorrespondentAccount = correspondentAccount,
                IsActive = true
            });

            result.ImportedRows++;
            result.Items.Add(new CounterpartyImportRowResultDto
            {
                RowNumber = rowNumber,
                Inn = inn,
                FullName = fullName,
                IsImported = true,
                Message = "Импортировано успешно."
            });
        }

        if (entitiesToAdd.Count > 0)
        {
            _dbContext.Counterparties.AddRange(entitiesToAdd);
            await _dbContext.SaveChangesAsync();
        }

        return result;
    }

    private async Task ValidateUniquenessAsync(UpsertCounterpartyDto dto, Guid organizationId)
    {
        var currentId = dto.Id ?? Guid.Empty;
        var normalizedInn = dto.Inn.Trim();
        var normalizedAccount = dto.AccountNumber.Trim();

        var exists = await _dbContext.Counterparties.AnyAsync(x =>
            x.OrganizationId == organizationId &&
            x.Id != currentId &&
            x.Inn == normalizedInn &&
            x.AccountNumber == normalizedAccount);

        if (exists)
            throw new InvalidOperationException("Контрагент с таким ИНН и счетом уже существует.");
    }

    private async Task EnsureInnUniqueAsync(Guid organizationId, string inn, Guid? excludeId = null)
    {
        inn = inn.Trim();

        var exists = await _dbContext.Counterparties
            .AsNoTracking()
            .AnyAsync(x =>
                x.OrganizationId == organizationId &&
                x.Inn == inn &&
                (!excludeId.HasValue || x.Id != excludeId.Value));

        if (exists)
            throw new InvalidOperationException($"Контрагент с INN {inn} уже существует в текущей организации.");
    }

    private static void ValidateBankDetails(UpsertCounterpartyDto dto)
    {
        var errors = ValidateCounterpartyImportRow(
            dto.Inn,
            dto.FullName,
            dto.Kpp,
            dto.Bic,
            dto.AccountNumber,
            dto.CorrespondentAccount);

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));
    }

    private static List<string> ValidateCounterpartyImportRow(
        string inn,
        string fullName,
        string kpp,
        string bic,
        string accountNumber,
        string correspondentAccount)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(inn))
            errors.Add("Не заполнен INN.");
        else if (!BankingValidators.IsValidInn(inn))
            errors.Add(BankingValidators.GetInnError(inn)!);

        if (string.IsNullOrWhiteSpace(fullName))
            errors.Add("Не заполнено наименование.");

        if (string.IsNullOrWhiteSpace(kpp))
            errors.Add("Не заполнен KPP.");
        else if (!BankingValidators.IsValidKpp(kpp))
            errors.Add(BankingValidators.GetKppError(kpp)!);

        if (string.IsNullOrWhiteSpace(bic))
            errors.Add("Не заполнен BIC.");
        else if (!BankingValidators.IsValidBic(bic))
            errors.Add(BankingValidators.GetBicError(bic)!);

        if (string.IsNullOrWhiteSpace(accountNumber))
            errors.Add("Не заполнен AccountNumber.");
        else if (!BankingValidators.IsValidSettlementAccount(bic, accountNumber))
            errors.Add(BankingValidators.GetSettlementAccountError(bic, accountNumber)!);

        if (string.IsNullOrWhiteSpace(correspondentAccount))
            errors.Add("Не заполнен CorrespondentAccount.");
        else if (!BankingValidators.IsValidCorrespondentAccount(bic, correspondentAccount))
            errors.Add(BankingValidators.GetCorrespondentAccountError(bic, correspondentAccount)!);

        return errors;
    }

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
}
