using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;

namespace OpenPay.Infrastructure.Banking;

public class SberDemoAdapter : IBankAdapter
{
    public string BankCode => "SBER";
    public string DisplayName => "Сбербанк Demo API";

    public Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment, BankConnection connection)
    {
        var reference = $"SB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];

        return Task.FromResult(new BankSubmitResultDto
        {
            IsAccepted = true,
            ReferenceId = reference,
            Message = "Сбербанк принял платеж к обработке."
        });
    }

    public Task<BankStatusResultDto> CheckPaymentStatusAsync(PaymentOrder payment, BankConnection connection)
    {
        var hasErrorMarker = payment.Purpose.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
                             payment.Purpose.Contains("ОШИБКА", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(new BankStatusResultDto
        {
            FinalStatus = hasErrorMarker ? PaymentStatus.Error : PaymentStatus.Executed,
            Message = hasErrorMarker
                ? "Сбербанк отклонил платеж в демо-сценарии."
                : "Сбербанк исполнил платеж в демо-сценарии."
        });
    }

    public Task<IReadOnlyList<BankStatementOperationDto>> LoadStatementAsync(
        OrganizationBankAccount account,
        BankConnection connection,
        DateOnly periodFrom,
        DateOnly periodTo)
    {
        IReadOnlyList<BankStatementOperationDto> operations =
        [
            new()
            {
                OperationId = $"SB-DEMO-{periodTo:yyyyMMdd}",
                OperationDate = periodTo,
                Amount = 2750,
                Currency = account.Currency,
                CounterpartyName = "Демо-операция Сбербанк",
                CounterpartyAccountNumber = "40702810888888888888",
                Purpose = "Несопоставленная операция из демо-выписки"
            }
        ];

        return Task.FromResult(operations);
    }
}
