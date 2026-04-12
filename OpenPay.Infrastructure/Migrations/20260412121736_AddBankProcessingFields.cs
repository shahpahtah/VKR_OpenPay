using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBankProcessingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankReferenceId",
                table: "PaymentOrders",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankResponseMessage",
                table: "PaymentOrders",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "PaymentOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "PaymentOrders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankReferenceId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "BankResponseMessage",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "PaymentOrders");
        }
    }
}
