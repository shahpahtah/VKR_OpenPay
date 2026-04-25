using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Infrastructure.Services;

public class OrganizationManagementService : IOrganizationManagementService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrganizationManagementService(
        OpenPayDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<OrganizationListItemDto>> GetAllAsync()
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new OrganizationListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(CreateOrganizationDto dto)
    {
        var normalizedInn = dto.Inn.Trim();
        var normalizedKpp = dto.Kpp.Trim();
        var normalizedEmail = dto.AdminEmail.Trim();

        if (await _dbContext.Organizations.AnyAsync(x => x.Inn == normalizedInn))
            throw new InvalidOperationException("Организация с таким ИНН уже существует.");

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser != null)
            throw new InvalidOperationException("Пользователь с таким email уже существует.");

        var organization = new Organization
        {
            Name = dto.Name.Trim(),
            Inn = normalizedInn,
            Kpp = normalizedKpp,
            IsActive = true
        };

        _dbContext.Organizations.Add(organization);
        await _dbContext.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FullName = dto.AdminFullName.Trim(),
            EmailConfirmed = true,
            Role = UserRole.Administrator,
            OrganizationId = organization.Id
        };

        var createResult = await _userManager.CreateAsync(user, dto.AdminPassword);

        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось создать администратора организации: {errors}");
        }

        var roleResult = await _userManager.AddToRoleAsync(user, UserRole.Administrator.ToString());

        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось назначить роль администратору: {errors}");
        }

        return organization.Id;
    }
}