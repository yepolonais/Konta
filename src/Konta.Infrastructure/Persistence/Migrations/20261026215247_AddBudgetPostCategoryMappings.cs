using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPostCategoryMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetPostCategoryMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnnualBudgetPostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPostCategoryMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetPostCategoryMappings_AnnualBudgetPosts_AnnualBudgetPostId",
                        column: x => x.AnnualBudgetPostId,
                        principalTable: "AnnualBudgetPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetPostCategoryMappings_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPostCategoryMappings_AnnualBudgetPostId",
                table: "BudgetPostCategoryMappings",
                column: "AnnualBudgetPostId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPostCategoryMappings_CategoryId",
                table: "BudgetPostCategoryMappings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPostCategoryMappings_Year_CategoryId",
                table: "BudgetPostCategoryMappings",
                columns: new[] { "Year", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetPostCategoryMappings");
        }
    }
}
