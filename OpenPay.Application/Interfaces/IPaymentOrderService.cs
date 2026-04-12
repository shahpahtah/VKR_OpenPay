using OpenPay.Application.DTOs.Payments;

namespace OpenPay.Application.Interfaces;

public interface IPaymentOrderService
{
    Task<IReadOnlyList<PaymentOrderListItemDto>> GetAllAsync(string? search = null);
    Task<UpsertPaymentOrderDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(UpsertPaymentOrderDto dto, string createdByUserId);
    Task UpdateAsync(UpsertPaymentOrderDto dto, string updatedByUserId);
}