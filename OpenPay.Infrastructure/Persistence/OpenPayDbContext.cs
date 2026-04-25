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

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();
    public DbSet<OrganizationBankAccount> OrganizationBankAccounts => Set<OrganizationBankAccount>();
    public DbSet<ApprovalRoute> ApprovalRoutes => Set<ApprovalRoute>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Organization>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Inn).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Kpp).HasMaxLength(9).IsRequired();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Counterparty>(entity =>
        {
            entity.Property(x => x.Inn).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Kpp).HasMaxLength(9);
            entity.Property(x => x.FullName).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Bic).HasMaxLength(9).IsRequired();
            entity.Property(x => x.AccountNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CorrespondentAccount).HasMaxLength(20).IsRequired();

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Counterparties)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrganizationBankAccount>(entity =>
        {
            entity.Property(x => x.Bic).HasMaxLength(9).IsRequired();
            entity.Property(x => x.AccountNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.BankName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.ResponsibleUnit).HasMaxLength(200).IsRequired();

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.BankAccounts)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApprovalRoute>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.ApprovalRoutes)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentOrder>(entity =>
        {
            entity.Property(x => x.DocumentNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Purpose).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.BankReferenceId).HasMaxLength(100);
            entity.Property(x => x.BankResponseMessage).HasMaxLength(2000);
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.PaymentOrders)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
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

        builder.Entity<BankStatement>(entity =>
        {
            entity.Property(x => x.RawDataJson).HasMaxLength(10000);
        });

        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ObjectId).HasMaxLength(100);
            entity.Property(x => x.ObjectType).HasMaxLength(200);
            entity.Property(x => x.IpAddress).HasMaxLength(100);

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.AuditLogEntries)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}