using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeBudgetPostsYearAgnostic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BudgetPostCategoryMappings_CategoryId",
                table: "BudgetPostCategoryMappings");

            migrationBuilder.DropIndex(
                name: "IX_BudgetPostCategoryMappings_Year_CategoryId",
                table: "BudgetPostCategoryMappings");

            migrationBuilder.DropIndex(
                name: "IX_BudgetMonthlyAmounts_AnnualBudgetPostId_Month",
                table: "BudgetMonthlyAmounts");

            migrationBuilder.DropIndex(
                name: "IX_AnnualBudgetPosts_Year_Name",
                table: "AnnualBudgetPosts");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "BudgetMonthlyAmounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Les postes deviennent permanents : l'année passe sur les montants et les postes de même nom sont fusionnés.
            migrationBuilder.Sql(@"
UPDATE BudgetMonthlyAmounts
SET Year = (SELECT p.Year FROM AnnualBudgetPosts p WHERE p.Id = BudgetMonthlyAmounts.AnnualBudgetPostId);

UPDATE BudgetMonthlyAmounts
SET AnnualBudgetPostId = (
    SELECT c.Id FROM AnnualBudgetPosts p
    JOIN AnnualBudgetPosts c ON lower(c.Name) = lower(p.Name)
    WHERE p.Id = BudgetMonthlyAmounts.AnnualBudgetPostId
    ORDER BY c.Year DESC, c.Id LIMIT 1);

DELETE FROM BudgetMonthlyAmounts
WHERE rowid NOT IN (SELECT MIN(rowid) FROM BudgetMonthlyAmounts GROUP BY AnnualBudgetPostId, Year, Month);

UPDATE BudgetPostCategoryMappings
SET AnnualBudgetPostId = (
    SELECT c.Id FROM AnnualBudgetPosts p
    JOIN AnnualBudgetPosts c ON lower(c.Name) = lower(p.Name)
    WHERE p.Id = BudgetPostCategoryMappings.AnnualBudgetPostId
    ORDER BY c.Year DESC, c.Id LIMIT 1);

DELETE FROM BudgetPostCategoryMappings
WHERE rowid NOT IN (SELECT MIN(rowid) FROM BudgetPostCategoryMappings GROUP BY CategoryId);

DELETE FROM AnnualBudgetPosts
WHERE Id NOT IN (
    SELECT (SELECT c.Id FROM AnnualBudgetPosts c WHERE lower(c.Name) = lower(p.Name) ORDER BY c.Year DESC, c.Id LIMIT 1)
    FROM AnnualBudgetPosts p);
");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "BudgetPostCategoryMappings");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "AnnualBudgetPosts");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPostCategoryMappings_CategoryId",
                table: "BudgetPostCategoryMappings",
                column: "CategoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMonthlyAmounts_AnnualBudgetPostId_Year_Month",
                table: "BudgetMonthlyAmounts",
                columns: new[] { "AnnualBudgetPostId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnualBudgetPosts_Name",
                table: "AnnualBudgetPosts",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BudgetPostCategoryMappings_CategoryId",
                table: "BudgetPostCategoryMappings");

            migrationBuilder.DropIndex(
                name: "IX_BudgetMonthlyAmounts_AnnualBudgetPostId_Year_Month",
                table: "BudgetMonthlyAmounts");

            migrationBuilder.DropIndex(
                name: "IX_AnnualBudgetPosts_Name",
                table: "AnnualBudgetPosts");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "BudgetMonthlyAmounts");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "BudgetPostCategoryMappings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "AnnualBudgetPosts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPostCategoryMappings_CategoryId",
                table: "BudgetPostCategoryMappings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPostCategoryMappings_Year_CategoryId",
                table: "BudgetPostCategoryMappings",
                columns: new[] { "Year", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMonthlyAmounts_AnnualBudgetPostId_Month",
                table: "BudgetMonthlyAmounts",
                columns: new[] { "AnnualBudgetPostId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnualBudgetPosts_Year_Name",
                table: "AnnualBudgetPosts",
                columns: new[] { "Year", "Name" },
                unique: true);
        }
    }
}
