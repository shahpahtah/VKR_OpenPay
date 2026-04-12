using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;

namespace OpenPay.Infrastructure.Banking;

public class FakeBankGatewayService : IBankGatewayService
{
    public Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment)
    {
        var result = new BankSubmitResultDto
        {
            IsAccepted = true,
            ReferenceId = $"BANK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32],
            Message = "Платеж принят банком к обработке."
        };

        return Task.FromResult(result);
    }

    public Task<BankStatusResultDto> CheckPaymentStatusAsync(PaymentOrder payment)
    {
        var purpose = payment.Purpose.ToUpperInvariant();

        var hasErrorMarker =
            purpose.Contains("ERROR") ||
            purpose.Contains("ОШИБКА");

        var result = new BankStatusResultDto
        {
            FinalStatus = hasErrorMarker
                ? PaymentStatus.Error
                : PaymentStatus.Executed,
            Message = hasErrorMarker
                ? "Банк отклонил платеж при обработке."
                : "Платеж успешно исполнен банком."
        };

        return Task.FromResult(result);
    }
}