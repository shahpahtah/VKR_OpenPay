using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;

namespace OpenPay.Infrastructure.Banking;

public class MockBankAdapter : IBankAdapter
{
    public string BankCode => "MOCK";
    public string DisplayName => "Mock Bank (заглушка)";

    public Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment, BankConnection connection)
    {
        var reference = $"MK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];

        return Task.FromResult(new BankSubmitResultDto
        {
            IsAccepted = true,
            ReferenceId = reference,
            Message = "Mock Bank принял платеж к обработке."
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
                ? "Mock Bank отклонил платеж в тестовом сценарии."
                : "Mock Bank исполнил платеж в тестовом сценарии."
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
                OperationId = $"MOCK-STMT-{periodTo:yyyyMMdd}",
                OperationDate = periodTo,
                Amount = 1990,
                Currency = account.Currency,
                CounterpartyName = "Mock-операция",
                CounterpartyAccountNumber = "40702810999999999999",
                Purpose = "Несопоставленная операция из mock-выписки"
            }
        ];

        return Task.FromResult(operations);
    }
}
