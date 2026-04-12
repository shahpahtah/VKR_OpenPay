using OpenPay.Application.DTOs.Accounts;

namespace OpenPay.Application.Interfaces;

public interface IOrganizationBankAccountService
{
    Task<IReadOnlyList<OrganizationBankAccountListItemDto>> GetAllAsync(string? search = null, bool? isActive = null);
    Task<UpsertOrganizationBankAccountDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(UpsertOrganizationBankAccountDto dto);
    Task UpdateAsync(UpsertOrganizationBankAccountDto dto);
    Task DeactivateAsync(Guid id);
}