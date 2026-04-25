namespace OpenPay.Application.Interfaces;

public interface ICurrentOrganizationService
{
    Task<Guid> GetRequiredOrganizationIdAsync();
    Task<Guid?> GetCurrentOrganizationIdAsync();
}