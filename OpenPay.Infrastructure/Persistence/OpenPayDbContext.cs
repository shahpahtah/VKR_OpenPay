using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenPay.Domain.Entities;
using OpenPay.Infrastructure.Security;

namespace OpenPay.Infrastructure.Persistence;

public class OpenPayDbContext : IdentityDbContext<ApplicationUser>
{
    public OpenPayDbContext(DbContextOptions<OpenPayDbContext> options)
        : base(options)
    {
    }

    public DbSet<Counterparty> Counterparties => Set<Counterparty>();
    public DbSet<OrganizationBankAccount> OrganizationBankAccounts => Set<OrganizationBankAccount>();
    public DbSet<ApprovalRoute> ApprovalRoutes => Set<ApprovalRoute>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Counterparty>(entity =>
        {
            entity.Property(x => x.Inn).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Kpp).HasMaxLength(9);
            entity.Property(x => x.FullName).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Bic).HasMaxLength(9).IsRequired();
            entity.Property(x => x.AccountNumber).HasMaxLength(20).IsRequired();
        });

        builder.Entity<OrganizationBankAccount>(entity =>
        {
            entity.Property(x => x.Bic).HasMaxLength(9).IsRequired();
            entity.Property(x => x.AccountNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.BankName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        });

        builder.Entity<PaymentOrder>(entity =>
        {
            entity.Property(x => x.DocumentNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Purpose).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.BankReferenceId).HasMaxLength(100);
            entity.Property(x => x.BankResponseMessage).HasMaxLength(2000);
        });

        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        });
        builder.Entity<ApprovalDecision>(entity =>
        {
            entity.Property(x => x.ApproverUserId).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(2000);

            entity.HasOne(x => x.PaymentOrder)
                .WithMany(x => x.ApprovalDecisions)
                .HasForeignKey(x => x.PaymentOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}