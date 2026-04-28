using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OpenPay.Application.DTOs.Banking;
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
            "ООО \"Открытые Платежи\"",
            "7710140679",
            "771001001");

        var secondOrganization = await EnsureOrganizationAsync(
            dbContext,
            "ООО \"Бета Трейд\"",
            "7704217370",
            "770401001");

        await EnsureUserAsync(
            userManager,
            "platformadmin@openpay.local",
            "Admin123!",
            "Администратор платформы",
            UserRole.PlatformAdmin,
            organization.Id);

        await EnsureUserAsync(
            userManager,
            "admin@openpay.local",
            "Admin123!",
            "Администратор организации",
            UserRole.Administrator,
            organization.Id);

        var accountant = await EnsureUserAsync(
            userManager,
            "accountant@openpay.local",
            "Accountant123!",
            "Бухгалтер демо",
            UserRole.Accountant,
            organization.Id);

        var manager = await EnsureUserAsync(
            userManager,
            "manager@openpay.local",
            "Manager123!",
            "Руководитель демо",
            UserRole.Manager,
            organization.Id);

        await EnsureUserAsync(
            userManager,
            "accountant2@openpay.local",
            "Accountant123!",
            "Бухгалтер второй организации",
            UserRole.Accountant,
            secondOrganization.Id);

        await EnsureUserAsync(
            userManager,
            "manager2@openpay.local",
            "Manager123!",
            "Руководитель второй организации",
            UserRole.Manager,
            secondOrganization.Id);

        await EnsureUserAsync(
            userManager,
            "admin2@openpay.local",
            "Admin123!",
            "Администратор второй организации",
            UserRole.Administrator,
            secondOrganization.Id);

        await EnsureDemoDataAsync(dbContext, tokenProtectionService, organization, accountant, manager);
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
        Organization organization,
        ApplicationUser accountant,
        ApplicationUser manager)
    {
        var tbankConnection = await EnsureBankConnectionAsync(
            dbContext,
            tokenProtectionService,
            organization.Id,
            "TBANK",
            "Основное подключение Т-Банк",
            "demo-tbank-access-token",
            "demo-tbank-refresh-token");

        var tbankSandboxConnection = await EnsureBankConnectionAsync(
            dbContext,
            tokenProtectionService,
            organization.Id,
            "TBANK_SANDBOX",
            "Основное подключение Т-Банк Sandbox",
            "TBankSandboxToken",
            string.Empty);

        var sberConnection = await EnsureBankConnectionAsync(
            dbContext,
            tokenProtectionService,
            organization.Id,
            "SBER",
            "Резервное подключение Сбербанк",
            "demo-sber-access-token",
            "demo-sber-refresh-token");

        var sandboxAccount = await EnsureBankAccountAsync(
            dbContext,
            organization.Id,
            tbankSandboxConnection.Id,
            "044525225",
            "40702810110011000009",
            "T-BANK",
            "RUB",
            "Бухгалтерия");

        var tbankDemoAccount = await EnsureBankAccountAsync(
            dbContext,
            organization.Id,
            tbankConnection.Id,
            "044525225",
            "40702810000000000023",
            "Т-Банк Demo",
            "RUB",
            "Бухгалтерия");

        var sberAccount = await EnsureBankAccountAsync(
            dbContext,
            organization.Id,
            sberConnection.Id,
            "044030653",
            "40702810000000000002",
            "Сбербанк Demo",
            "RUB",
            "Закупки");

        var romashka = await EnsureCounterpartyAsync(
            dbContext,
            organization.Id,
            "7707083893",
            "770701001",
            "ООО \"Ромашка\"",
            "044525225",
            "40702810000000000007",
            "30101810400000000225");

        var vector = await EnsureCounterpartyAsync(
            dbContext,
            organization.Id,
            "500100732259",
            "500101001",
            "ООО \"Вектор\"",
            "044030653",
            "40702810000000000028",
            "30101810500000000653");

        var sever = await EnsureCounterpartyAsync(
            dbContext,
            organization.Id,
            "5408117935",
            "540801001",
            "ООО \"Север Логистика\"",
            "044525225",
            "40702810000000000007",
            "30101810400000000225");

        var standardRoute = await EnsureApprovalRouteAsync(
            dbContext,
            organization.Id,
            "Стандартный маршрут до 100 000",
            0,
            100000,
            null,
            null,
            ApprovalType.Sequential);

        var largeRoute = await EnsureApprovalRouteAsync(
            dbContext,
            organization.Id,
            "Крупные платежи",
            100000.01m,
            null,
            null,
            null,
            ApprovalType.Parallel);

        var rentRoute = await EnsureApprovalRouteAsync(
            dbContext,
            organization.Id,
            "Аренда и офисные расходы бухгалтерии",
            0,
            300000,
            "Аренда",
            "Бухгалтерия",
            ApprovalType.Sequential);

        var procurementRoute = await EnsureApprovalRouteAsync(
            dbContext,
            organization.Id,
            "Закупки до 500 000",
            0,
            500000,
            "Поставщики",
            "Закупки",
            ApprovalType.Parallel);

        var now = DateTime.UtcNow;
        var today = DateTime.UtcNow.Date;

        var draftPayment = await EnsurePaymentAsync(
            dbContext,
            organization.Id,
            "PAY-DEMO-001",
            today.AddDays(2),
            romashka.Id,
            sandboxAccount.Id,
            standardRoute.Id,
            accountant.Id,
            12500.50m,
            "RUB",
            "Поставщики",
            "Оплата поставщику по договору №15",
            PaymentStatus.Draft,
            now.AddDays(-5));

        var pendingPayment = await EnsurePaymentAsync(
            dbContext,
            organization.Id,
            "PAY-DEMO-002",
            today.AddDays(4),
            sever.Id,
            sandboxAccount.Id,
            rentRoute.Id,
            accountant.Id,
            78000m,
            "RUB",
            "Аренда",
            "Аренда офиса за май",
            PaymentStatus.PendingApproval,
            now.AddDays(-4));

        var readyPayment = await EnsurePaymentAsync(
            dbContext,
            organization.Id,
            "PAY-DEMO-003",
            today.AddDays(1),
            vector.Id,
            sberAccount.Id,
            procurementRoute.Id,
            accountant.Id,
            48600m,
            "RUB",
            "Поставщики",
            "Закупка оборудования для отдела продаж",
            PaymentStatus.ReadyToSend,
            now.AddDays(-3),
            signedAt: now.AddDays(-1),
            signatureReference: "SIGN-DEMO-READY-001",
            bankResponseMessage: "Платеж подписан и ожидает ручной fallback-отправки.");

        var executedPayment = await EnsurePaymentAsync(
            dbContext,
            organization.Id,
            "PAY-DEMO-004",
            today.AddDays(-1),
            romashka.Id,
            sandboxAccount.Id,
            standardRoute.Id,
            accountant.Id,
            32100m,
            "RUB",
            "Поставщики",
            "Оплата поставки канцелярии",
            PaymentStatus.Executed,
            now.AddDays(-8),
            bankReferenceId: "demo-bank-ref-executed-001",
            signedAt: now.AddDays(-2),
            signatureReference: "SIGN-DEMO-EXECUTED-001",
            sentAt: now.AddDays(-2),
            processedAt: now.AddDays(-1),
            bankResponseMessage: "Т-Банк Sandbox вернул статус EXECUTED.");

        var errorPayment = await EnsurePaymentAsync(
            dbContext,
            organization.Id,
            "PAY-DEMO-005",
            today.AddDays(-2),
            vector.Id,
            sberAccount.Id,
            standardRoute.Id,
            accountant.Id,
            15900m,
            "RUB",
            "Поставщики",
            "Оплата тестового счета с ошибкой банка",
            PaymentStatus.Error,
            now.AddDays(-7),
            signedAt: now.AddDays(-6),
            signatureReference: "SIGN-DEMO-ERROR-001",
            sentAt: now.AddDays(-6),
            processedAt: now.AddDays(-6),
            bankResponseMessage: "Демо-банк отклонил платеж в тестовом сценарии.");

        var reworkPayment = await EnsurePaymentAsync(
            dbContext,
            organization.Id,
            "PAY-DEMO-006",
            today.AddDays(5),
            sever.Id,
            tbankDemoAccount.Id,
            standardRoute.Id,
            accountant.Id,
            8800m,
            "RUB",
            "Логистика",
            "Перевозка документов",
            PaymentStatus.Rework,
            now.AddDays(-2),
            bankResponseMessage: "Руководитель попросил уточнить назначение платежа.");

        var largePayment = await EnsurePaymentAsync(
            dbContext,
            organization.Id,
            "PAY-DEMO-007",
            today.AddDays(7),
            vector.Id,
            sberAccount.Id,
            largeRoute.Id,
            accountant.Id,
            240000m,
            "RUB",
            "Поставщики",
            "Крупная закупка серверного оборудования",
            PaymentStatus.PendingApproval,
            now.AddDays(-1));

        await EnsureApprovalDecisionAsync(dbContext, readyPayment.Id, manager.Id, ApprovalDecisionType.Approved, "Согласовано для отправки.", now.AddDays(-1));
        await EnsureApprovalDecisionAsync(dbContext, executedPayment.Id, manager.Id, ApprovalDecisionType.Approved, "Согласовано.", now.AddDays(-2));
        await EnsureApprovalDecisionAsync(dbContext, errorPayment.Id, manager.Id, ApprovalDecisionType.Approved, "Согласовано, ошибка получена от банка.", now.AddDays(-6));
        await EnsureApprovalDecisionAsync(dbContext, reworkPayment.Id, manager.Id, ApprovalDecisionType.Rework, "Уточнить основание платежа.", now.AddDays(-1));

        await EnsureDemoStatementAsync(dbContext, organization.Id, sandboxAccount.Id, executedPayment);
        await EnsureDemoAuditAsync(
            dbContext,
            organization.Id,
            accountant.Id,
            manager.Id,
            draftPayment,
            pendingPayment,
            executedPayment,
            errorPayment,
            largePayment);
    }

    private static async Task<OrganizationBankAccount> EnsureBankAccountAsync(
        OpenPayDbContext dbContext,
        Guid organizationId,
        Guid bankConnectionId,
        string bic,
        string accountNumber,
        string bankName,
        string currency,
        string responsibleUnit)
    {
        var account = await dbContext.OrganizationBankAccounts
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.AccountNumber == accountNumber);

        if (account == null)
        {
            account = new OrganizationBankAccount
            {
                OrganizationId = organizationId,
                AccountNumber = accountNumber
            };

            dbContext.OrganizationBankAccounts.Add(account);
        }

        account.BankConnectionId = bankConnectionId;
        account.Bic = bic;
        account.BankName = bankName;
        account.Currency = currency;
        account.ResponsibleUnit = responsibleUnit;
        account.IsActive = true;

        await dbContext.SaveChangesAsync();
        return account;
    }

    private static async Task<Counterparty> EnsureCounterpartyAsync(
        OpenPayDbContext dbContext,
        Guid organizationId,
        string inn,
        string kpp,
        string fullName,
        string bic,
        string accountNumber,
        string correspondentAccount)
    {
        var counterparty = await dbContext.Counterparties
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Inn == inn);

        if (counterparty == null)
        {
            counterparty = new Counterparty
            {
                OrganizationId = organizationId,
                Inn = inn
            };

            dbContext.Counterparties.Add(counterparty);
        }

        counterparty.Kpp = kpp;
        counterparty.FullName = fullName;
        counterparty.Bic = bic;
        counterparty.AccountNumber = accountNumber;
        counterparty.CorrespondentAccount = correspondentAccount;
        counterparty.IsActive = true;

        await dbContext.SaveChangesAsync();
        return counterparty;
    }

    private static async Task<ApprovalRoute> EnsureApprovalRouteAsync(
        OpenPayDbContext dbContext,
        Guid organizationId,
        string name,
        decimal? minAmount,
        decimal? maxAmount,
        string? expenseType,
        string? department,
        ApprovalType approvalType)
    {
        var route = await dbContext.ApprovalRoutes
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Name == name);

        if (route == null)
        {
            route = new ApprovalRoute
            {
                OrganizationId = organizationId,
                Name = name
            };

            dbContext.ApprovalRoutes.Add(route);
        }

        route.MinAmount = minAmount;
        route.MaxAmount = maxAmount;
        route.ExpenseType = expenseType;
        route.Department = department;
        route.ApprovalType = approvalType;
        route.IsActive = true;

        await dbContext.SaveChangesAsync();
        return route;
    }

    private static async Task<PaymentOrder> EnsurePaymentAsync(
        OpenPayDbContext dbContext,
        Guid organizationId,
        string documentNumber,
        DateTime paymentDate,
        Guid counterpartyId,
        Guid organizationBankAccountId,
        Guid approvalRouteId,
        string createdByUserId,
        decimal amount,
        string currency,
        string expenseType,
        string purpose,
        PaymentStatus status,
        DateTime createdAt,
        string? bankReferenceId = null,
        DateTime? signedAt = null,
        string? signatureReference = null,
        DateTime? sentAt = null,
        DateTime? processedAt = null,
        string? bankResponseMessage = null)
    {
        var payment = await dbContext.PaymentOrders
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.DocumentNumber == documentNumber);

        if (payment == null)
        {
            payment = new PaymentOrder
            {
                OrganizationId = organizationId,
                DocumentNumber = documentNumber
            };

            dbContext.PaymentOrders.Add(payment);
        }

        payment.CreatedAt = createdAt;
        payment.PaymentDate = paymentDate;
        payment.CounterpartyId = counterpartyId;
        payment.OrganizationBankAccountId = organizationBankAccountId;
        payment.ApprovalRouteId = approvalRouteId;
        payment.CreatedByUserId = createdByUserId;
        payment.Amount = amount;
        payment.Currency = currency;
        payment.ExpenseType = expenseType;
        payment.Purpose = purpose;
        payment.Status = status;
        payment.BankReferenceId = bankReferenceId;
        payment.SignedAt = signedAt;
        payment.SignatureReference = signatureReference;
        payment.SentAt = sentAt;
        payment.ProcessedAt = processedAt;
        payment.BankResponseMessage = bankResponseMessage;

        await dbContext.SaveChangesAsync();
        return payment;
    }

    private static async Task EnsureApprovalDecisionAsync(
        OpenPayDbContext dbContext,
        Guid paymentOrderId,
        string approverUserId,
        ApprovalDecisionType decision,
        string comment,
        DateTime createdAt)
    {
        var exists = await dbContext.ApprovalDecisions.AnyAsync(x =>
            x.PaymentOrderId == paymentOrderId &&
            x.ApproverUserId == approverUserId &&
            x.Decision == decision);

        if (exists)
            return;

        dbContext.ApprovalDecisions.Add(new ApprovalDecision
        {
            PaymentOrderId = paymentOrderId,
            ApproverUserId = approverUserId,
            Decision = decision,
            Comment = comment,
            CreatedAt = createdAt
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureDemoStatementAsync(
        OpenPayDbContext dbContext,
        Guid organizationId,
        Guid accountId,
        PaymentOrder matchedPayment)
    {
        var periodFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-14));
        var periodTo = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var statement = await dbContext.BankStatements
            .FirstOrDefaultAsync(x =>
                x.OrganizationId == organizationId &&
                x.OrganizationBankAccountId == accountId &&
                x.PeriodFrom == periodFrom &&
                x.PeriodTo == periodTo);

        if (statement == null)
        {
            statement = new BankStatement
            {
                OrganizationId = organizationId,
                OrganizationBankAccountId = accountId,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo
            };

            dbContext.BankStatements.Add(statement);
        }

        var operations = new List<BankStatementOperationDto>
        {
            new()
            {
                OperationId = "DEMO-STMT-MATCHED-001",
                OperationDate = DateOnly.FromDateTime(matchedPayment.PaymentDate ?? DateTime.UtcNow.Date),
                Amount = matchedPayment.Amount,
                Currency = matchedPayment.Currency,
                CounterpartyName = "ООО \"Ромашка\"",
                CounterpartyAccountNumber = "40702810000000000007",
                Purpose = matchedPayment.Purpose,
                BankReferenceId = matchedPayment.BankReferenceId
            },
            new()
            {
                OperationId = "DEMO-STMT-UNMATCHED-001",
                OperationDate = periodTo,
                Amount = 1990m,
                Currency = "RUB",
                CounterpartyName = "ООО \"Тестовая операция\"",
                CounterpartyAccountNumber = "40702810999999999999",
                Purpose = "Несопоставленная операция из демо-выписки",
                BankReferenceId = "demo-unmatched-ref-001"
            }
        };

        statement.CreatedAt = DateTime.UtcNow.AddHours(-6);
        statement.RawDataJson = JsonSerializer.Serialize(operations, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        statement.TotalOperations = operations.Count;
        statement.MatchedOperations = 1;
        statement.UnmatchedOperations = 1;

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureDemoAuditAsync(
        OpenPayDbContext dbContext,
        Guid organizationId,
        string accountantUserId,
        string managerUserId,
        params PaymentOrder[] payments)
    {
        if (await dbContext.AuditLogEntries.AnyAsync(x => x.OrganizationId == organizationId))
            return;

        var now = DateTime.UtcNow;
        var entries = new List<AuditLogEntry>
        {
            new()
            {
                OrganizationId = organizationId,
                CreatedAt = now.AddDays(-5),
                EventType = AuditEventType.PaymentCreated,
                UserId = accountantUserId,
                Description = $"Создано платежное поручение {payments[0].DocumentNumber}",
                ObjectId = payments[0].Id.ToString(),
                ObjectType = nameof(PaymentOrder)
            },
            new()
            {
                OrganizationId = organizationId,
                CreatedAt = now.AddDays(-4),
                EventType = AuditEventType.PaymentSubmittedForApproval,
                UserId = accountantUserId,
                Description = $"Платеж {payments[1].DocumentNumber} отправлен на согласование",
                ObjectId = payments[1].Id.ToString(),
                ObjectType = nameof(PaymentOrder)
            },
            new()
            {
                OrganizationId = organizationId,
                CreatedAt = now.AddDays(-2),
                EventType = AuditEventType.PaymentApproved,
                UserId = managerUserId,
                Description = $"Платеж {payments[2].DocumentNumber} утвержден",
                ObjectId = payments[2].Id.ToString(),
                ObjectType = nameof(PaymentOrder)
            },
            new()
            {
                OrganizationId = organizationId,
                CreatedAt = now.AddDays(-1),
                EventType = AuditEventType.PaymentExecutedByBank,
                UserId = accountantUserId,
                Description = $"Платеж {payments[2].DocumentNumber} исполнен банком",
                ObjectId = payments[2].Id.ToString(),
                ObjectType = nameof(PaymentOrder)
            },
            new()
            {
                OrganizationId = organizationId,
                CreatedAt = now.AddHours(-12),
                EventType = AuditEventType.PaymentBankError,
                UserId = accountantUserId,
                Description = $"Платеж {payments[3].DocumentNumber} завершился ошибкой банка",
                ObjectId = payments[3].Id.ToString(),
                ObjectType = nameof(PaymentOrder)
            },
            new()
            {
                OrganizationId = organizationId,
                CreatedAt = now.AddHours(-6),
                EventType = AuditEventType.BankStatementReconciled,
                UserId = accountantUserId,
                Description = "Выполнена сверка демо-выписки: найдено 1, не найдено 1",
                ObjectType = nameof(BankStatement)
            },
            new()
            {
                OrganizationId = organizationId,
                CreatedAt = now.AddHours(-3),
                EventType = AuditEventType.PaymentSubmittedForApproval,
                UserId = accountantUserId,
                Description = $"Крупный платеж {payments[4].DocumentNumber} отправлен на согласование",
                ObjectId = payments[4].Id.ToString(),
                ObjectType = nameof(PaymentOrder)
            }
        };

        dbContext.AuditLogEntries.AddRange(entries);
        await dbContext.SaveChangesAsync();
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

    private static async Task<ApplicationUser> EnsureUserAsync(
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

            if (user.OrganizationId != organizationId)
            {
                user.OrganizationId = organizationId;
                needUpdate = true;
            }

            if (user.Role != role)
            {
                user.Role = role;
                needUpdate = true;
            }

            if (user.FullName != fullName)
            {
                user.FullName = fullName;
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

        return user;
    }
}
