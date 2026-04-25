using OpenPay.Application.DTOs.Admin;

namespace OpenPay.Application.Interfaces;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserListItemDto>> GetUsersForCurrentOrganizationAsync();
    Task CreateUserAsync(CreateUserDto dto);
    Task<UpdateUserDto?> GetByIdAsync(string id);
    Task UpdateAsync(UpdateUserDto dto);
    Task DeactivateAsync(string id);
    Task ActivateAsync(string id);
}