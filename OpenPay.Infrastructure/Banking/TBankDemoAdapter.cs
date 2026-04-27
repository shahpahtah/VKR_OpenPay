using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;

namespace OpenPay.Infrastructure.Banking;

public class TBankDemoAdapter : IBankAdapter
{
    public string BankCode => "TBANK";
    public string DisplayName => "Т-Банк Demo API";

    public Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment, BankConnection connection)
    {
        var reference = $"TB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];

        return Task.FromResult(new BankSubmitResultDto
        {
            IsAccepted = true,
            ReferenceId = reference,
            Message = "Т-Банк принял платеж к обработке."
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
                ? "Т-Банк отклонил платеж в демо-сценарии."
                : "Т-Банк исполнил платеж в демо-сценарии."
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
                OperationId = $"TB-DEMO-{periodTo:yyyyMMdd}",
                OperationDate = periodTo,
                Amount = 1990,
                Currency = account.Currency,
                CounterpartyName = "Демо-операция Т-Банк",
                CounterpartyAccountNumber = "40702810999999999999",
                Purpose = "Несопоставленная операция из демо-выписки"
            }
        ];

        return Task.FromResult(operations);
    }
}
