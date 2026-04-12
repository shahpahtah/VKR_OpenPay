using Microsoft.AspNetCore.Identity;
using OpenPay.Domain.Enums;

namespace OpenPay.Infrastructure.Security;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var roles = new[]
        {
            UserRole.Accountant.ToString(),
            UserRole.Manager.ToString(),
            UserRole.Administrator.ToString()
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(
            userManager,
            "admin@openpay.local",
            "Admin123!",
            "System Administrator",
            UserRole.Administrator);

        await EnsureUserAsync(
            userManager,
            "accountant@openpay.local",
            "Accountant123!",
            "Demo Accountant",
            UserRole.Accountant);

        await EnsureUserAsync(
            userManager,
            "manager@openpay.local",
            "Manager123!",
            "Demo Manager",
            UserRole.Manager);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        UserRole role)
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
                Role = role
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Не удалось создать пользователя {email}: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role.ToString()))
        {
            await userManager.AddToRoleAsync(user, role.ToString());
        }
    }
}