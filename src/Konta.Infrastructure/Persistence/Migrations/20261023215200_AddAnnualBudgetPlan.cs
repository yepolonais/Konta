using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnualBudgetPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnualBudgetPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualBudgetPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetMonthlyAmounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnnualBudgetPostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetMonthlyAmounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetMonthlyAmounts_AnnualBudgetPosts_AnnualBudgetPostId",
                        column: x => x.AnnualBudgetPostId,
                        principalTable: "AnnualBudgetPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnualBudgetPosts_Year_Name",
                table: "AnnualBudgetPosts",
                columns: new[] { "Year", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMonthlyAmounts_AnnualBudgetPostId_Month",
                table: "BudgetMonthlyAmounts",
                columns: new[] { "AnnualBudgetPostId", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetMonthlyAmounts");

            migrationBuilder.DropTable(
                name: "AnnualBudgetPosts");
        }
    }
}
