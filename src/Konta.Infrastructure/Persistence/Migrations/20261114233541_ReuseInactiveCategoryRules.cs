using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReuseInactiveCategoryRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CategoryRules_Name",
                table: "CategoryRules");

            migrationBuilder.DropIndex(
                name: "IX_CategoryRules_Pattern_OperationType",
                table: "CategoryRules");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_Name",
                table: "CategoryRules",
                column: "Name",
                unique: true,
                filter: "\"IsActive\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_Pattern_OperationType",
                table: "CategoryRules",
                columns: new[] { "Pattern", "OperationType" },
                unique: true,
                filter: "\"IsActive\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CategoryRules_Name",
                table: "CategoryRules");

            migrationBuilder.DropIndex(
                name: "IX_CategoryRules_Pattern_OperationType",
                table: "CategoryRules");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_Name",
                table: "CategoryRules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_Pattern_OperationType",
                table: "CategoryRules",
                columns: new[] { "Pattern", "OperationType" },
                unique: true);
        }
    }
}
