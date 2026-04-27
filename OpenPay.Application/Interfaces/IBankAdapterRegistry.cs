using OpenPay.Application.DTOs.Banking;

namespace OpenPay.Application.Interfaces;

public interface IBankAdapterRegistry
{
    IReadOnlyList<BankAdapterInfoDto> GetAvailableAdapters();
    IBankAdapter GetRequiredAdapter(string bankCode);
}
