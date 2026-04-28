using OpenPay.Application.DTOs.Organizations;

namespace OpenPay.Application.Interfaces;

public interface ICurrentOrganizationService
{
    Task<Guid> GetRequiredOrganizationIdAsync();
    Task<Guid?> GetCurrentOrganizationIdAsync();
    Task<CurrentOrganizationDto?> GetCurrentOrganizationInfoAsync();
}
