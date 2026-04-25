using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Tests;

public static class TestDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
       

        var db = scope.ServiceProvider.GetRequiredService<OpenPayDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (await db.Organizations.AnyAsync())
            return;

        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var org1 = new Organization
        {
            Name = "ООО Альфа",
            Inn = "7701111111",
            Kpp = "770101001",
            IsActive = true
        };

        var org2 = new Organization
        {
            Name = "ООО Бета",
            Inn = "7802222222",
            Kpp = "780201001",
            IsActive = true
        };

        db.Organizations.AddRange(org1, org2);
        await db.SaveChangesAsync();

        var accountant1 = new ApplicationUser
        {
            UserName = "acc1@test.local",
            Email = "acc1@test.local",
            FullName = "Accountant One",
            EmailConfirmed = true,
            Role = UserRole.Accountant,
            OrganizationId = org1.Id,
            IsActive = true
        };

        var accountant2 = new ApplicationUser
        {
            UserName = "acc2@test.local",
            Email = "acc2@test.local",
            FullName = "Accountant Two",
            EmailConfirmed = true,
            Role = UserRole.Accountant,
            OrganizationId = org2.Id,
            IsActive = true
        };

        await userManager.CreateAsync(accountant1, "Password1!");
        await userManager.CreateAsync(accountant2, "Password1!");

        await userManager.AddToRoleAsync(accountant1, UserRole.Accountant.ToString());
        await userManager.AddToRoleAsync(accountant2, UserRole.Accountant.ToString());

        db.Counterparties.AddRange(
            new Counterparty
            {
                OrganizationId = org1.Id,
                Inn = "7703000001",
                Kpp = "770301001",
                FullName = "Контрагент Альфа",
                Bic = "044525225",
                AccountNumber = "40702810000000000001",
                CorrespondentAccount = "30101810400000000225",
                IsActive = true
            },
            new Counterparty
            {
                OrganizationId = org2.Id,
                Inn = "7804000002",
                Kpp = "780401001",
                FullName = "Контрагент Бета",
                Bic = "044525225",
                AccountNumber = "40702810000000000002",
                CorrespondentAccount = "30101810400000000225",
                IsActive = true
            });

        await db.SaveChangesAsync();
    }
}