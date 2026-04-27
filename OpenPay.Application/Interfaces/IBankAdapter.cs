using OpenPay.Application.DTOs.Banking;
using OpenPay.Domain.Entities;

namespace OpenPay.Application.Interfaces;

public interface IBankAdapter
{
    string BankCode { get; }
    string DisplayName { get; }

    Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment, BankConnection connection);
    Task<BankStatusResultDto> CheckPaymentStatusAsync(PaymentOrder payment, BankConnection connection);
    Task<IReadOnlyList<BankStatementOperationDto>> LoadStatementAsync(
        OrganizationBankAccount account,
        BankConnection connection,
        DateOnly periodFrom,
        DateOnly periodTo);
}
