using OpenPay.Application.DTOs.Admin;

namespace OpenPay.Application.Interfaces;

public interface IOrganizationManagementService
{
    Task<IReadOnlyList<OrganizationListItemDto>> GetAllAsync();
    Task<Guid> CreateAsync(CreateOrganizationDto dto);
}