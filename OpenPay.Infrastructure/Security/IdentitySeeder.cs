using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Security;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        OpenPayDbContext dbContext)
    {
        var roles = new[]
        {
            UserRole.Accountant.ToString(),
            UserRole.Manager.ToString(),
            UserRole.Administrator.ToString(),
            UserRole.PlatformAdmin.ToString()
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(x => x.Inn == "7701234567");

        if (organization == null)
        {
            organization = new Organization
            {
                Name = "ООО \"Компания\"",
                Inn = "7701234567",
                Kpp = "770101001",
                IsActive = true
            };

            dbContext.Organizations.Add(organization);
            await dbContext.SaveChangesAsync();
        }
        var secondOrganization = await dbContext.Organizations
        .FirstOrDefaultAsync(x => x.Inn == "7801234567");

        if (secondOrganization == null)
        {
            secondOrganization = new Organization
            {
                Name = "ООО \"Бета\"",
                Inn = "7801234567",
                Kpp = "780101001",
                IsActive = true
            };

            dbContext.Organizations.Add(secondOrganization);
            await dbContext.SaveChangesAsync();
        }
        await EnsureUserAsync(
            userManager,
            "platformadmin@openpay.local",
            "Admin123!",
            "Platform Administrator",
            UserRole.PlatformAdmin,
            organization.Id);

        await EnsureUserAsync(
            userManager,
            "admin@openpay.local",
            "Admin123!",
            "System Administrator",
            UserRole.Administrator,
            organization.Id);

        await EnsureUserAsync(
            userManager,
            "accountant@openpay.local",
            "Accountant123!",
            "Demo Accountant",
            UserRole.Accountant,
            organization.Id);

        await EnsureUserAsync(
            userManager,
            "manager@openpay.local",
            "Manager123!",
            "Demo Manager",
            UserRole.Manager,
            organization.Id);
        await EnsureUserAsync(
            userManager,
            "accountant2@openpay.local",
            "Accountant123!",
            "Second Accountant",
            UserRole.Accountant,
            secondOrganization.Id);

        await EnsureUserAsync(
            userManager,
            "manager2@openpay.local",
            "Manager123!",
            "Second Manager",
            UserRole.Manager,
            secondOrganization.Id);

        await EnsureUserAsync(
            userManager,
            "admin2@openpay.local",
            "Admin123!",
            "Second Administrator",
            UserRole.Administrator,
            secondOrganization.Id);
    }
        

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        UserRole role,
        Guid organizationId)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                Role = role,
                OrganizationId = organizationId,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Не удалось создать пользователя {email}: {errors}");
            }
        }
        else
        {
            var needUpdate = false;

            if (user.OrganizationId == null)
            {
                user.OrganizationId = organizationId;
                needUpdate = true;
            }

            if (user.Role != role)
            {
                user.Role = role;
                needUpdate = true;
            }

            if (needUpdate)
            {
                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Не удалось обновить пользователя {email}: {errors}");
                }
            }
        }

        if (!await userManager.IsInRoleAsync(user, role.ToString()))
        {
            await userManager.AddToRoleAsync(user, role.ToString());
        }
    }
}