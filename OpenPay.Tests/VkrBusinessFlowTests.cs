using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.DTOs.Audit;
using OpenPay.Application.DTOs.BankStatements;
using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.DTOs.Organizations;
using OpenPay.Application.DTOs.Payments;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Banking;
using OpenPay.Infrastructure.Persistence;
using OpenPay.Infrastructure.Services;
using Xunit;

namespace OpenPay.Tests;

public class VkrBusinessFlowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Create_payment_should_assign_specific_route_and_block_duplicate()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await CreateDbContextAsync(connection);
        var seed = await SeedPaymentReferencesAsync(db);

        var generalRoute = new ApprovalRoute
        {
            OrganizationId = seed.OrganizationId,
            Name = "Общий маршрут",
            MinAmount = 0,
            MaxAmount = 100000,
            ApprovalType = ApprovalType.Sequential,
            IsActive = true
        };
        var specificRoute = new ApprovalRoute
        {
            OrganizationId = seed.OrganizationId,
            Name = "Аренда бухгалтерии",
            MinAmount = 0,
            MaxAmount = 100000,
            ExpenseType = "Аренда",
            Department = "Бухгалтерия",
            ApprovalType = ApprovalType.Parallel,
            IsActive = true
        };

        db.ApprovalRoutes.AddRange(generalRoute, specificRoute);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = CreatePaymentOrderService(db, seed.OrganizationId);
        var dto = new UpsertPaymentOrderDto
        {
            PaymentDate = new DateTime(2026, 5, 10),
            CounterpartyId = seed.CounterpartyId,
            OrganizationBankAccountId = seed.AccountId,
            Amount = 25000,
            Currency = "RUB",
            ExpenseType = "Аренда",
            Purpose = "Оплата аренды офиса"
        };

        var paymentId = await service.CreateAsync(dto, "accountant-1");

        var payment = await db.PaymentOrders.AsNoTracking().SingleAsync(x => x.Id == paymentId);
        payment.ApprovalRouteId.Should().Be(specificRoute.Id);

        var duplicateCreate = () => service.CreateAsync(dto, "accountant-1");
        await duplicateCreate.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*дубликат*");
    }

    [Fact]
    public async Task Approval_should_create_demo_signature_and_send_payment_to_bank_adapter()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await CreateDbContextAsync(connection);
        var seed = await SeedPaymentReferencesAsync(db, withBankConnection: true);

        var payment = new PaymentOrder
        {
            OrganizationId = seed.OrganizationId,
            DocumentNumber = "PAY-APPROVE-001",
            CreatedAt = DateTime.UtcNow,
            PaymentDate = new DateTime(2026, 5, 12),
            CounterpartyId = seed.CounterpartyId,
            OrganizationBankAccountId = seed.AccountId,
            Amount = 12000,
            Currency = "RUB",
            ExpenseType = "Поставщики",
            Purpose = "Оплата поставщику",
            Status = PaymentStatus.PendingApproval,
            CreatedByUserId = "accountant-1"
        };

        db.PaymentOrders.Add(payment);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentOrganization = new FixedCurrentOrganizationService(seed.OrganizationId);
        var audit = new NoOpAuditLogService();
        var bankAdapter = new AcceptingBankAdapter();
        var registry = new BankAdapterRegistry([bankAdapter]);
        var gateway = new BankGatewayService(registry);
        var bankProcessing = new BankProcessingService(db, gateway, audit, currentOrganization);
        var approvalService = new ApprovalService(db, audit, currentOrganization, bankProcessing);

        await approvalService.ApproveAsync(payment.Id, "manager-1", "Согласовано");

        var savedPayment = await db.PaymentOrders.AsNoTracking().SingleAsync(x => x.Id == payment.Id);
        savedPayment.Status.Should().Be(PaymentStatus.Sent);
        savedPayment.SignedAt.Should().NotBeNull();
        savedPayment.SignatureReference.Should().StartWith("SIGN-");
        savedPayment.BankReferenceId.Should().Be(AcceptingBankAdapter.ReferenceId);
        savedPayment.SentAt.Should().NotBeNull();

        var decision = await db.ApprovalDecisions.AsNoTracking().SingleAsync(x => x.PaymentOrderId == payment.Id);
        decision.Decision.Should().Be(ApprovalDecisionType.Approved);
    }

    [Fact]
    public async Task Bank_statement_reconciliation_should_match_payment_by_bank_reference()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await CreateDbContextAsync(connection);
        var seed = await SeedPaymentReferencesAsync(db, withBankConnection: true);

        var payment = new PaymentOrder
        {
            OrganizationId = seed.OrganizationId,
            DocumentNumber = "PAY-REC-001",
            CreatedAt = DateTime.UtcNow,
            PaymentDate = new DateTime(2026, 5, 14),
            CounterpartyId = seed.CounterpartyId,
            OrganizationBankAccountId = seed.AccountId,
            Amount = 9800,
            Currency = "RUB",
            ExpenseType = "Поставщики",
            Purpose = "Оплата по счету 77",
            Status = PaymentStatus.Sent,
            BankReferenceId = "BANK-STATEMENT-REF",
            CreatedByUserId = "accountant-1"
        };

        var operations = new List<BankStatementOperationDto>
        {
            new()
            {
                OperationId = "OP-001",
                OperationDate = DateOnly.FromDateTime(payment.PaymentDate!.Value),
                Amount = payment.Amount,
                Currency = payment.Currency,
                CounterpartyName = "ООО Ромашка",
                CounterpartyAccountNumber = "40702810000000000007",
                Purpose = payment.Purpose,
                BankReferenceId = payment.BankReferenceId
            }
        };

        var statement = new BankStatement
        {
            OrganizationId = seed.OrganizationId,
            OrganizationBankAccountId = seed.AccountId,
            PeriodFrom = new DateOnly(2026, 5, 1),
            PeriodTo = new DateOnly(2026, 5, 31),
            RawDataJson = JsonSerializer.Serialize(operations, JsonOptions),
            TotalOperations = 1,
            UnmatchedOperations = 1
        };

        db.PaymentOrders.Add(payment);
        db.BankStatements.Add(statement);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new BankStatementService(
            db,
            new BankAdapterRegistry([new AcceptingBankAdapter()]),
            new NoOpAuditLogService(),
            new FixedCurrentOrganizationService(seed.OrganizationId));

        var result = await service.ReconcileAsync(statement.Id, "accountant-1");

        result.TotalOperations.Should().Be(1);
        result.MatchedOperations.Should().Be(1);
        result.UnmatchedOperations.Should().Be(0);
        result.Items.Single().MatchedDocumentNumber.Should().Be(payment.DocumentNumber);

        var savedPayment = await db.PaymentOrders.AsNoTracking().SingleAsync(x => x.Id == payment.Id);
        savedPayment.Status.Should().Be(PaymentStatus.Executed);
    }

    private static async Task<OpenPayDbContext> CreateDbContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OpenPayDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new OpenPayDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static PaymentOrderService CreatePaymentOrderService(OpenPayDbContext db, Guid organizationId) =>
        new(
            db,
            new NoOpAuditLogService(),
            new FixedCurrentOrganizationService(organizationId));

    private static async Task<SeedReferences> SeedPaymentReferencesAsync(
        OpenPayDbContext db,
        bool withBankConnection = false)
    {
        var organization = new Organization
        {
            Name = "ООО Компания",
            Inn = "7707083893",
            Kpp = "770701001",
            IsActive = true
        };

        var counterparty = new Counterparty
        {
            OrganizationId = organization.Id,
            Inn = "500100732259",
            Kpp = "500101001",
            FullName = "ООО Ромашка",
            Bic = "044525225",
            AccountNumber = "40702810000000000007",
            CorrespondentAccount = "30101810400000000225",
            IsActive = true
        };

        BankConnection? bankConnection = null;
        if (withBankConnection)
        {
            bankConnection = new BankConnection
            {
                OrganizationId = organization.Id,
                BankCode = AcceptingBankAdapter.BankCodeValue,
                DisplayName = "Тестовое подключение",
                ProtectedAccessToken = "token",
                ProtectedRefreshToken = string.Empty,
                IsActive = true
            };
        }

        var account = new OrganizationBankAccount
        {
            OrganizationId = organization.Id,
            BankConnectionId = bankConnection?.Id,
            Bic = "044525225",
            AccountNumber = "40702810000000000009",
            BankName = "Тестовый банк",
            Currency = "RUB",
            ResponsibleUnit = "Бухгалтерия",
            IsActive = true
        };

        db.Organizations.Add(organization);
        db.Counterparties.Add(counterparty);
        if (bankConnection != null)
            db.BankConnections.Add(bankConnection);
        db.OrganizationBankAccounts.Add(account);

        await db.SaveChangesAsync();
        return new SeedReferences(organization.Id, counterparty.Id, account.Id);
    }

    private sealed record SeedReferences(Guid OrganizationId, Guid CounterpartyId, Guid AccountId);

    private sealed class FixedCurrentOrganizationService : ICurrentOrganizationService
    {
        private readonly Guid _organizationId;

        public FixedCurrentOrganizationService(Guid organizationId)
        {
            _organizationId = organizationId;
        }

        public Task<Guid> GetRequiredOrganizationIdAsync() => Task.FromResult(_organizationId);

        public Task<Guid?> GetCurrentOrganizationIdAsync() => Task.FromResult<Guid?>(_organizationId);

        public Task<CurrentOrganizationDto?> GetCurrentOrganizationInfoAsync() =>
            Task.FromResult<CurrentOrganizationDto?>(new CurrentOrganizationDto
            {
                Id = _organizationId,
                Name = "ООО Компания",
                Inn = "7707083893",
                IsActive = true
            });
    }

    private sealed class NoOpAuditLogService : IAuditLogService
    {
        public Task LogAsync(
            AuditEventType eventType,
            string? userId,
            string description,
            string? objectId = null,
            string? objectType = null) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AuditLogListItemDto>> GetAllAsync(AuditLogFilterDto? filter = null) =>
            Task.FromResult<IReadOnlyList<AuditLogListItemDto>>([]);
    }

    private sealed class AcceptingBankAdapter : IBankAdapter
    {
        public const string BankCodeValue = "TEST_BANK";
        public const string ReferenceId = "BANK-REF-001";

        public string BankCode => BankCodeValue;
        public string DisplayName => "Тестовый банк";

        public Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment, BankConnection connection) =>
            Task.FromResult(new BankSubmitResultDto
            {
                IsAccepted = true,
                ReferenceId = ReferenceId,
                Message = "Тестовый банк принял платеж."
            });

        public Task<BankStatusResultDto> CheckPaymentStatusAsync(PaymentOrder payment, BankConnection connection) =>
            Task.FromResult(new BankStatusResultDto
            {
                FinalStatus = PaymentStatus.Executed,
                Message = "Тестовый банк исполнил платеж."
            });

        public Task<IReadOnlyList<BankStatementOperationDto>> LoadStatementAsync(
            OrganizationBankAccount account,
            BankConnection connection,
            DateOnly periodFrom,
            DateOnly periodTo) =>
            Task.FromResult<IReadOnlyList<BankStatementOperationDto>>([]);
    }
}
