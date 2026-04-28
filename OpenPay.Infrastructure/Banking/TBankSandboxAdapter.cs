using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;

namespace OpenPay.Infrastructure.Banking;

public class TBankSandboxAdapter : IBankAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ITokenProtectionService _tokenProtectionService;

    public TBankSandboxAdapter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ITokenProtectionService tokenProtectionService)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _tokenProtectionService = tokenProtectionService;
    }

    public string BankCode => "TBANK_SANDBOX";
    public string DisplayName => "Т-Банк Sandbox API";

    public async Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment, BankConnection connection)
    {
        if (payment.Counterparty == null || payment.OrganizationBankAccount == null)
        {
            return new BankSubmitResultDto
            {
                IsAccepted = false,
                Message = "Для отправки платежа нужны контрагент и счет организации."
            };
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildUri(GetPaymentBaseUrl(), "/api/v1/payment/ruble-transfer/pay"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetAccessToken(connection));
        request.Content = JsonContent.Create(BuildPaymentRequest(payment), options: JsonOptions);

        try
        {
            using var response = await SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new BankSubmitResultDto
                {
                    IsAccepted = false,
                    Message = $"Т-Банк Sandbox вернул HTTP {(int)response.StatusCode}: {TrimForMessage(body)}"
                };
            }

            var reference = ExtractString(body, "paymentId", "id", "operationId", "referenceId")
                            ?? payment.Id.ToString("N");

            return new BankSubmitResultDto
            {
                IsAccepted = true,
                ReferenceId = reference,
                Message = "Т-Банк Sandbox принял платеж к обработке."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            return new BankSubmitResultDto
            {
                IsAccepted = false,
                Message = $"Не удалось обратиться к Т-Банк Sandbox ({request.RequestUri}): {FormatException(ex)}"
            };
        }
    }

    public async Task<BankStatusResultDto> CheckPaymentStatusAsync(PaymentOrder payment, BankConnection connection)
    {
        if (string.IsNullOrWhiteSpace(payment.BankReferenceId))
        {
            return new BankStatusResultDto
            {
                FinalStatus = PaymentStatus.Error,
                Message = "У платежа нет банковского идентификатора для проверки статуса."
            };
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUri(GetPaymentBaseUrl(), $"/api/v1/payment/{Uri.EscapeDataString(payment.BankReferenceId)}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetAccessToken(connection));

        try
        {
            using var response = await SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new BankStatusResultDto
                {
                    FinalStatus = PaymentStatus.Error,
                    Message = $"Т-Банк Sandbox не вернул статус платежа. HTTP {(int)response.StatusCode}: {TrimForMessage(body)}"
                };
            }

            var status = ExtractString(body, "status", "paymentStatus", "state");
            var finalStatus = MapPaymentStatus(status);

            return new BankStatusResultDto
            {
                FinalStatus = finalStatus,
                Message = string.IsNullOrWhiteSpace(status)
                    ? "Т-Банк Sandbox вернул статус платежа без явного поля status."
                    : $"Т-Банк Sandbox вернул статус {status}."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            return new BankStatusResultDto
            {
                FinalStatus = PaymentStatus.Error,
                Message = $"Не удалось проверить статус в Т-Банк Sandbox ({request.RequestUri}): {FormatException(ex)}"
            };
        }
    }

    public async Task<IReadOnlyList<BankStatementOperationDto>> LoadStatementAsync(
        OrganizationBankAccount account,
        BankConnection connection,
        DateOnly periodFrom,
        DateOnly periodTo)
    {
        var from = periodFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var till = periodTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var query = $"accountNumber={Uri.EscapeDataString(account.AccountNumber)}&from={Uri.EscapeDataString(from)}&till={Uri.EscapeDataString(till)}";
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUri(GetStatementBaseUrl(), $"/api/v1/bank-statement?{query}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetAccessToken(connection));

        using var response = await SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Т-Банк Sandbox не вернул выписку. HTTP {(int)response.StatusCode}: {TrimForMessage(body)}");

        return ParseStatementOperations(body, account.Currency);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var client = _httpClientFactory.CreateClient("TBankSandbox");
        return await client.SendAsync(request);
    }

    private object BuildPaymentRequest(PaymentOrder payment)
    {
        var counterparty = payment.Counterparty!;
        var account = payment.OrganizationBankAccount!;
        var documentNumber = string.IsNullOrWhiteSpace(payment.DocumentNumber)
            ? payment.Id.ToString("N")[..8]
            : payment.DocumentNumber;

        return new
        {
            id = payment.Id.ToString("N"),
            amount = payment.Amount,
            from = new
            {
                accountNumber = account.AccountNumber
            },
            to = new
            {
                name = counterparty.FullName,
                inn = counterparty.Inn,
                kpp = string.IsNullOrWhiteSpace(counterparty.Kpp) ? "0" : counterparty.Kpp,
                bik = counterparty.Bic,
                corrAccountNumber = counterparty.CorrespondentAccount,
                accountNumber = counterparty.AccountNumber
            },
            purpose = payment.Purpose,
            executionOrder = 5,
            meta = new
            {
                openPayDocumentNumber = documentNumber
            }
        };
    }

    private string GetAccessToken(BankConnection connection)
    {
        var token = TryUnprotect(connection.ProtectedAccessToken);

        if (!string.IsNullOrWhiteSpace(token))
            return token;

        return _configuration["TBankSandbox:DefaultAccessToken"] ?? "TBankSandboxToken";
    }

    private string TryUnprotect(string protectedToken)
    {
        if (string.IsNullOrWhiteSpace(protectedToken))
            return string.Empty;

        try
        {
            return _tokenProtectionService.Unprotect(protectedToken);
        }
        catch (CryptographicException)
        {
            return protectedToken;
        }
    }

    private string GetPaymentBaseUrl() =>
        _configuration["TBankSandbox:PaymentBaseUrl"] ?? "https://business.tbank.ru/openapi/sandbox/secured";

    private string GetStatementBaseUrl() =>
        _configuration["TBankSandbox:StatementBaseUrl"] ?? "https://business.tbank.ru/openapi/sandbox";

    private static Uri BuildUri(string baseUrl, string path)
    {
        var normalizedBase = baseUrl.TrimEnd('/');
        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        return new Uri(normalizedBase + normalizedPath, UriKind.Absolute);
    }

    private static PaymentStatus MapPaymentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return PaymentStatus.Executed;

        var normalized = status.Trim().ToUpperInvariant();

        if (normalized is "EXECUTED" or "COMPLETED" or "DONE" or "SUCCESS" or "SUCCEEDED")
            return PaymentStatus.Executed;

        if (normalized is "FAILED" or "REJECTED" or "ERROR" or "DECLINED" or "CANCELED" or "CANCELLED")
            return PaymentStatus.Error;

        return PaymentStatus.Executed;
    }

    private static IReadOnlyList<BankStatementOperationDto> ParseStatementOperations(string body, string defaultCurrency)
    {
        using var document = JsonDocument.Parse(body);
        var operations = new List<BankStatementOperationDto>();

        foreach (var item in EnumerateOperationObjects(document.RootElement))
        {
            var amount = ExtractDecimal(item, "amount", "paymentAmount", "operationAmount", "sum");

            if (amount == null)
                continue;

            operations.Add(new BankStatementOperationDto
            {
                OperationId = ExtractString(item, "operationId", "id", "paymentId") ?? Guid.NewGuid().ToString("N"),
                OperationDate = ExtractDate(item, "operationDate", "date", "operationDateTime", "paymentDate") ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = Math.Abs(amount.Value),
                Currency = ExtractString(item, "currency", "accountCurrency") ?? defaultCurrency,
                CounterpartyName = ExtractString(item, "counterpartyName", "recipientName", "payerName", "name") ?? "-",
                CounterpartyAccountNumber = ExtractString(item, "counterpartyAccountNumber", "recipientAccount", "payerAccount", "accountNumber") ?? string.Empty,
                Purpose = ExtractString(item, "purpose", "paymentPurpose", "description") ?? string.Empty,
                BankReferenceId = ExtractString(item, "bankReferenceId", "paymentId", "operationId", "id")
            });
        }

        return operations;
    }

    private static IEnumerable<JsonElement> EnumerateOperationObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (LooksLikeOperation(element))
                yield return element;

            foreach (var property in element.EnumerateObject())
            {
                foreach (var child in EnumerateOperationObjects(property.Value))
                    yield return child;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in EnumerateOperationObjects(item))
                    yield return child;
            }
        }
    }

    private static bool LooksLikeOperation(JsonElement element) =>
        ExtractDecimal(element, "amount", "paymentAmount", "operationAmount", "sum") != null &&
        (ExtractString(element, "purpose", "paymentPurpose", "description") != null ||
         ExtractString(element, "operationId", "id", "paymentId") != null);

    private static string? ExtractString(string body, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        using var document = JsonDocument.Parse(body);
        return ExtractString(document.RootElement, names);
    }

    private static string? ExtractString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(name, out var value) &&
                value.ValueKind != JsonValueKind.Null &&
                value.ValueKind != JsonValueKind.Undefined)
            {
                return value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : value.ToString();
            }
        }

        return null;
    }

    private static decimal? ExtractDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return number;

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var stringNumber))
            {
                return stringNumber;
            }

            if (value.ValueKind == JsonValueKind.Object)
            {
                var nested = ExtractDecimal(value, "value", "amount", "sum");
                if (nested != null)
                    return nested;
            }
        }

        return null;
    }

    private static DateOnly? ExtractDate(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = ExtractString(element, name);

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
                return DateOnly.FromDateTime(dateTime);

            if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
                return dateOnly;
        }

        return null;
    }

    private static string TrimForMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "пустой ответ";

        return body.Length <= 500 ? body : body[..500] + "...";
    }
    private static string FormatException(Exception exception)
    {
        var messages = new List<string>();

        for (var current = exception; current != null; current = current.InnerException)
            messages.Add($"{current.GetType().Name}: {current.Message}");

        return string.Join(" -> ", messages);
    }
}
