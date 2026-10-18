using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneActiveSavingsGoalPerAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavingsGoals_AccountId_IsActive",
                table: "SavingsGoals");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoals_AccountId",
                table: "SavingsGoals",
                column: "AccountId",
                unique: true,
                filter: "\"IsActive\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavingsGoals_AccountId",
                table: "SavingsGoals");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoals_AccountId_IsActive",
                table: "SavingsGoals",
                columns: new[] { "AccountId", "IsActive" });
        }
    }
}
