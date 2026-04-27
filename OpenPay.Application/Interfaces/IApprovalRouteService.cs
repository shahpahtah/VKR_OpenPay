using OpenPay.Application.DTOs.ApprovalRoutes;

namespace OpenPay.Application.Interfaces;

public interface IApprovalRouteService
{
    Task<IReadOnlyList<ApprovalRouteListItemDto>> GetAllAsync(bool includeInactive = false);
    Task<UpsertApprovalRouteDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(UpsertApprovalRouteDto dto, string userId);
    Task UpdateAsync(UpsertApprovalRouteDto dto, string userId);
    Task DeactivateAsync(Guid id, string userId);
}
