using OpenPay.Application.DTOs.BankStatements;

namespace OpenPay.Application.Interfaces;

public interface IBankStatementService
{
    Task<IReadOnlyList<BankStatementListItemDto>> GetAllAsync();
    Task<BankStatementResultDto> LoadDemoStatementAsync(
        Guid organizationBankAccountId,
        DateOnly periodFrom,
        DateOnly periodTo,
        string userId);
    Task<BankStatementResultDto> ReconcileAsync(Guid bankStatementId, string userId);
}
