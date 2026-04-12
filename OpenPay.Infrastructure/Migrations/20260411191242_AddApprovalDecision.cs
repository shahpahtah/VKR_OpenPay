using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApproverUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Decision = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_PaymentOrders_PaymentOrderId",
                        column: x => x.PaymentOrderId,
                        principalTable: "PaymentOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_PaymentOrderId",
                table: "ApprovalDecisions",
                column: "PaymentOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalDecisions");
        }
    }
}
