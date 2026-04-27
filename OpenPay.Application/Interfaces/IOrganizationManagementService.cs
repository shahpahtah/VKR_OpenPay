using OpenPay.Application.DTOs.Admin;

namespace OpenPay.Application.Interfaces;

public interface IOrganizationManagementService
{
    Task<IReadOnlyList<OrganizationListItemDto>> GetAllAsync();
    Task<Guid> CreateAsync(CreateOrganizationDto dto);
    Task DeactivateAsync(Guid id);
    Task ActivateAsync(Guid id);
}