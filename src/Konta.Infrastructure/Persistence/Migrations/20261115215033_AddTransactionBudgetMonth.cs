using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionBudgetMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BudgetMonth",
                table: "Transactions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql("""
                UPDATE "Transactions"
                SET "BudgetMonth" = substr("Date", 1, 7) || '-01';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BudgetMonth",
                table: "Transactions",
                column: "BudgetMonth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_BudgetMonth",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BudgetMonth",
                table: "Transactions");
        }
    }
}
