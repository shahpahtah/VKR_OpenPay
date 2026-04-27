using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Security;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        OpenPayDbContext dbContext,
        ITokenProtectionService tokenProtectionService)
    {
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var organization = await EnsureOrganizationAsync(
            dbContext,
            "ООО \"Компания\"",
            "7701234567",
            "770101001");

        var secondOrganization = await EnsureOrganizationAsync(
            dbContext,
            "ООО \"Бета\"",
            "7801234567",
            "780101001");

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

        await EnsureDemoDataAsync(dbContext, tokenProtectionService, organization);
    }

    private static async Task<Organization> EnsureOrganizationAsync(
        OpenPayDbContext dbContext,
        string name,
        string inn,
        string kpp)
    {
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(x => x.Inn == inn);

        if (organization != null)
            return organization;

        organization = new Organization
        {
            Name = name,
            Inn = inn,
            Kpp = kpp,
            IsActive = true
        };

        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync();

        return organization;
    }

    private static async Task EnsureDemoDataAsync(
        OpenPayDbContext dbContext,
        ITokenProtectionService tokenProtectionService,
        Organization organization)
    {
        var tbankConnection = await EnsureBankConnectionAsync(
            dbContext,
            tokenProtectionService,
            organization.Id,
            "TBANK",
            "Основное подключение Т-Банк",
            "demo-tbank-access-token",
            "demo-tbank-refresh-token");

        var sberConnection = await EnsureBankConnectionAsync(
            dbContext,
            tokenProtectionService,
            organization.Id,
            "SBER",
            "Резервное подключение Сбербанк",
            "demo-sber-access-token",
            "demo-sber-refresh-token");

        if (!await dbContext.OrganizationBankAccounts.AnyAsync(x => x.OrganizationId == organization.Id))
        {
            dbContext.OrganizationBankAccounts.AddRange(
                new OrganizationBankAccount
                {
                    OrganizationId = organization.Id,
                    BankConnectionId = tbankConnection.Id,
                    Bic = "044525225",
                    AccountNumber = "40702810000000000007",
                    BankName = "Т-Банк",
                    Currency = "RUB",
                    ResponsibleUnit = "Бухгалтерия",
                    IsActive = true
                },
                new OrganizationBankAccount
                {
                    OrganizationId = organization.Id,
                    BankConnectionId = sberConnection.Id,
                    Bic = "044030653",
                    AccountNumber = "40702810000000000002",
                    BankName = "Сбербанк",
                    Currency = "RUB",
                    ResponsibleUnit = "Закупки",
                    IsActive = true
                });

            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Counterparties.AnyAsync(x => x.OrganizationId == organization.Id))
        {
            dbContext.Counterparties.AddRange(
                new Counterparty
                {
                    OrganizationId = organization.Id,
                    Inn = "7707083893",
                    Kpp = "770701001",
                    FullName = "ООО \"Ромашка\"",
                    Bic = "044525225",
                    AccountNumber = "40702810000000000007",
                    CorrespondentAccount = "30101810400000000225",
                    IsActive = true
                },
                new Counterparty
                {
                    OrganizationId = organization.Id,
                    Inn = "500100732259",
                    Kpp = "500101001",
                    FullName = "ООО \"Вектор\"",
                    Bic = "044030653",
                    AccountNumber = "40702810000000000002",
                    CorrespondentAccount = "30101810500000000653",
                    IsActive = true
                });

            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.ApprovalRoutes.AnyAsync(x => x.OrganizationId == organization.Id))
        {
            dbContext.ApprovalRoutes.AddRange(
                new ApprovalRoute
                {
                    OrganizationId = organization.Id,
                    Name = "Стандартный маршрут до 100 000",
                    MinAmount = 0,
                    MaxAmount = 100000,
                    ApprovalType = ApprovalType.Sequential,
                    IsActive = true
                },
                new ApprovalRoute
                {
                    OrganizationId = organization.Id,
                    Name = "Крупные платежи",
                    MinAmount = 100000.01m,
                    ApprovalType = ApprovalType.Parallel,
                    IsActive = true
                });

            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task<BankConnection> EnsureBankConnectionAsync(
        OpenPayDbContext dbContext,
        ITokenProtectionService tokenProtectionService,
        Guid organizationId,
        string bankCode,
        string displayName,
        string accessToken,
        string refreshToken)
    {
        var connection = await dbContext.BankConnections
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.BankCode == bankCode);

        if (connection != null)
            return connection;

        connection = new BankConnection
        {
            OrganizationId = organizationId,
            BankCode = bankCode,
            DisplayName = displayName,
            ProtectedAccessToken = tokenProtectionService.Protect(accessToken),
            ProtectedRefreshToken = tokenProtectionService.Protect(refreshToken),
            IsActive = true
        };

        dbContext.BankConnections.Add(connection);
        await dbContext.SaveChangesAsync();

        return connection;
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
                throw new InvalidOperationException($"Не удалось создать пользователя {email}: {errors}");
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

            if (!user.IsActive)
            {
                user.IsActive = true;
                needUpdate = true;
            }

            if (needUpdate)
            {
                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Не удалось обновить пользователя {email}: {errors}");
                }
            }
        }

        if (!await userManager.IsInRoleAsync(user, role.ToString()))
            await userManager.AddToRoleAsync(user, role.ToString());
    }
}
