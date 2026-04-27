using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCounterpartyInnPerOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Counterparties_OrganizationId",
                table: "Counterparties");

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_OrganizationId_Inn",
                table: "Counterparties",
                columns: new[] { "OrganizationId", "Inn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Counterparties_OrganizationId_Inn",
                table: "Counterparties");

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_OrganizationId",
                table: "Counterparties",
                column: "OrganizationId");
        }
    }
}
