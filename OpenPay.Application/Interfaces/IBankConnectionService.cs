using OpenPay.Application.DTOs.BankConnections;
using OpenPay.Application.DTOs.Banking;

namespace OpenPay.Application.Interfaces;

public interface IBankConnectionService
{
    IReadOnlyList<BankAdapterInfoDto> GetAvailableBanks();
    Task<IReadOnlyList<BankConnectionListItemDto>> GetAllAsync(bool includeInactive = false);
    Task<UpsertBankConnectionDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(UpsertBankConnectionDto dto, string userId);
    Task UpdateAsync(UpsertBankConnectionDto dto, string userId);
    Task DeactivateAsync(Guid id, string userId);
}
