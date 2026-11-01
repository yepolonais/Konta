using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleOperationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CategoryRules_Pattern",
                table: "CategoryRules");

            migrationBuilder.AddColumn<string>(
                name: "OperationType",
                table: "CategoryRules",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_Pattern_OperationType",
                table: "CategoryRules",
                columns: new[] { "Pattern", "OperationType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CategoryRules_Pattern_OperationType",
                table: "CategoryRules");

            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "CategoryRules");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_Pattern",
                table: "CategoryRules",
                column: "Pattern",
                unique: true);
        }
    }
}
