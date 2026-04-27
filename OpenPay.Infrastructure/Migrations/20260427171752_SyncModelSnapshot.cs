using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpenseType",
                table: "PaymentOrders",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignatureReference",
                table: "PaymentOrders",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "PaymentOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankConnectionId",
                table: "OrganizationBankAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BankStatements",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "MatchedOperations",
                table: "BankStatements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "BankStatements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalOperations",
                table: "BankStatements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnmatchedOperations",
                table: "BankStatements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogEntries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "BankConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BankCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProtectedAccessToken = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ProtectedRefreshToken = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankConnections_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationBankAccounts_BankConnectionId",
                table: "OrganizationBankAccounts",
                column: "BankConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_OrganizationId",
                table: "BankStatements",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_BankConnections_OrganizationId_DisplayName",
                table: "BankConnections",
                columns: new[] { "OrganizationId", "DisplayName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BankStatements_Organizations_OrganizationId",
                table: "BankStatements",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationBankAccounts_BankConnections_BankConnectionId",
                table: "OrganizationBankAccounts",
                column: "BankConnectionId",
                principalTable: "BankConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankStatements_Organizations_OrganizationId",
                table: "BankStatements");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationBankAccounts_BankConnections_BankConnectionId",
                table: "OrganizationBankAccounts");

            migrationBuilder.DropTable(
                name: "BankConnections");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationBankAccounts_BankConnectionId",
                table: "OrganizationBankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_BankStatements_OrganizationId",
                table: "BankStatements");

            migrationBuilder.DropColumn(
                name: "ExpenseType",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "SignatureReference",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "BankConnectionId",
                table: "OrganizationBankAccounts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BankStatements");

            migrationBuilder.DropColumn(
                name: "MatchedOperations",
                table: "BankStatements");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "BankStatements");

            migrationBuilder.DropColumn(
                name: "TotalOperations",
                table: "BankStatements");

            migrationBuilder.DropColumn(
                name: "UnmatchedOperations",
                table: "BankStatements");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
