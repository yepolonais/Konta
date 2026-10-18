using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetsForecastsAndBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountBalanceReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BalanceAfterTransaction = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBalanceReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountBalanceReferences_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountBalanceReferences_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyBudgets_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LabelPattern = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    DayOfMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringForecasts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringForecasts_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BudgetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetOverrides_MonthlyBudgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "MonthlyBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForecastOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ForecastId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastOccurrences_RecurringForecasts_ForecastId",
                        column: x => x.ForecastId,
                        principalTable: "RecurringForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ForecastOccurrences_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalanceReferences_AccountId",
                table: "AccountBalanceReferences",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalanceReferences_TransactionId",
                table: "AccountBalanceReferences",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetOverrides_BudgetId_Year_Month",
                table: "BudgetOverrides",
                columns: new[] { "BudgetId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForecastOccurrences_ForecastId_ScheduledDate",
                table: "ForecastOccurrences",
                columns: new[] { "ForecastId", "ScheduledDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForecastOccurrences_TransactionId",
                table: "ForecastOccurrences",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyBudgets_CategoryId",
                table: "MonthlyBudgets",
                column: "CategoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringForecasts_AccountId",
                table: "RecurringForecasts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringForecasts_CategoryId",
                table: "RecurringForecasts",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountBalanceReferences");

            migrationBuilder.DropTable(
                name: "BudgetOverrides");

            migrationBuilder.DropTable(
                name: "ForecastOccurrences");

            migrationBuilder.DropTable(
                name: "MonthlyBudgets");

            migrationBuilder.DropTable(
                name: "RecurringForecasts");
        }
    }
}
