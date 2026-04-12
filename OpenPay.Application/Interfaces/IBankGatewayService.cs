using OpenPay.Application.DTOs.Banking;
using OpenPay.Domain.Entities;

namespace OpenPay.Application.Interfaces;

public interface IBankGatewayService
{
    Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment);
    Task<BankStatusResultDto> CheckPaymentStatusAsync(PaymentOrder payment);
}