using OpenPay.Application.DTOs.Counterparties;

namespace OpenPay.Application.Interfaces;

public interface ICounterpartyService
{
    Task<IReadOnlyList<CounterpartyListItemDto>> GetAllAsync(string? search = null, bool? isActive = null);
    Task<UpsertCounterpartyDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(UpsertCounterpartyDto dto);
    Task UpdateAsync(UpsertCounterpartyDto dto);
    Task DeactivateAsync(Guid id);
    Task<CounterpartyImportResultDto> ImportFromCsvAsync(Stream csvStream);
}