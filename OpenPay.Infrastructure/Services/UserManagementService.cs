using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly OpenPayDbContext _dbContext;
    private readonly ICurrentOrganizationService _currentOrganizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        OpenPayDbContext dbContext,
        ICurrentOrganizationService currentOrganizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _currentOrganizationService = currentOrganizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetUsersForCurrentOrganizationAsync()
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Organization)
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.FullName)
            .Select(x => new UserListItemDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email ?? string.Empty,
                Role = x.Role.ToString(),
                OrganizationName = x.Organization != null ? x.Organization.Name : null,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task CreateUserAsync(CreateUserDto dto)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var email = dto.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            throw new InvalidOperationException("Пользователь с таким email уже существует.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = dto.FullName.Trim(),
            EmailConfirmed = true,
            Role = dto.Role,
            OrganizationId = organizationId,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось создать пользователя: {errors}");
        }

        var roleName = dto.Role.ToString();
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);

        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось назначить роль: {errors}");
        }
    }

    public async Task<UpdateUserDto?> GetByIdAsync(string id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == id && x.OrganizationId == organizationId)
            .Select(x => new UpdateUserDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email ?? string.Empty,
                Role = x.Role,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(UpdateUserDto dto)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == dto.Id && x.OrganizationId == organizationId);

        if (user == null)
            throw new InvalidOperationException("Пользователь не найден.");

        var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!dto.IsActive && currentUserId == user.Id)
            throw new InvalidOperationException("Нельзя деактивировать самого себя.");

        var normalizedEmail = dto.Email.Trim();

        var emailOwner = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.Id != dto.Id);

        if (emailOwner != null)
            throw new InvalidOperationException("Пользователь с таким email уже существует.");

        user.FullName = dto.FullName.Trim();
        user.Email = normalizedEmail;
        user.UserName = normalizedEmail;
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось обновить пользователя: {errors}");
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        if (existingRoles.Count > 0)
        {
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
            if (!removeRolesResult.Succeeded)
            {
                var errors = string.Join("; ", removeRolesResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Не удалось обновить роли пользователя: {errors}");
            }
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, dto.Role.ToString());
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось назначить новую роль: {errors}");
        }

        await ApplyActivationStateAsync(user, dto.IsActive);
    }

    public async Task DeactivateAsync(string id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId);

        if (user == null)
            throw new InvalidOperationException("Пользователь не найден.");

        var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == user.Id)
            throw new InvalidOperationException("Нельзя деактивировать самого себя.");

        user.IsActive = false;
        await ApplyActivationStateAsync(user, false);
    }

    public async Task ActivateAsync(string id)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId);

        if (user == null)
            throw new InvalidOperationException("Пользователь не найден.");

        user.IsActive = true;
        await ApplyActivationStateAsync(user, true);
    }

    private async Task ApplyActivationStateAsync(ApplicationUser user, bool isActive)
    {
        user.IsActive = isActive;

        if (isActive)
        {
            user.LockoutEnd = null;
            user.LockoutEnabled = true;
        }
        else
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось изменить активность пользователя: {errors}");
        }
    }
}