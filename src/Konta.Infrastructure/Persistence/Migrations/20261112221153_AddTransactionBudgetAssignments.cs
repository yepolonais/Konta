using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionBudgetAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetDescription",
                table: "Transactions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BudgetPostId",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMultiPost",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BudgetPostId",
                table: "Transactions",
                column: "BudgetPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_AnnualBudgetPosts_BudgetPostId",
                table: "Transactions",
                column: "BudgetPostId",
                principalTable: "AnnualBudgetPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE "Transactions"
                SET "BudgetPostId" = (
                    SELECT "AnnualBudgetPostId"
                    FROM "BudgetPostCategoryMappings"
                    WHERE "BudgetPostCategoryMappings"."CategoryId" = "Transactions"."CategoryId"
                )
                WHERE "CategoryId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_AnnualBudgetPosts_BudgetPostId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BudgetPostId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BudgetDescription",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BudgetPostId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsMultiPost",
                table: "Categories");
        }
    }
}
