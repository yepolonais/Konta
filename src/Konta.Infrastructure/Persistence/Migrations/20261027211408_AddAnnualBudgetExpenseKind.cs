using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnualBudgetExpenseKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpenseKind",
                table: "AnnualBudgetPosts",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE AnnualBudgetPosts SET ExpenseKind = 'Variable' WHERE Type = 'Expense';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpenseKind",
                table: "AnnualBudgetPosts");
        }
    }
}
