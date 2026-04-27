using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;

namespace OpenPay.Infrastructure.Banking;

public class BankAdapterRegistry : IBankAdapterRegistry
{
    private readonly IReadOnlyDictionary<string, IBankAdapter> _adapters;

    public BankAdapterRegistry(IEnumerable<IBankAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(
            x => x.BankCode,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<BankAdapterInfoDto> GetAvailableAdapters() =>
        _adapters.Values
            .OrderBy(x => x.DisplayName)
            .Select(x => new BankAdapterInfoDto
            {
                BankCode = x.BankCode,
                DisplayName = x.DisplayName
            })
            .ToList();

    public IBankAdapter GetRequiredAdapter(string bankCode)
    {
        if (_adapters.TryGetValue(bankCode, out var adapter))
            return adapter;

        throw new InvalidOperationException($"Банковский адаптер {bankCode} не зарегистрирован.");
    }
}
