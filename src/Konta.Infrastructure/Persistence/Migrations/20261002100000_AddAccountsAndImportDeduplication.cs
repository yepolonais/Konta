using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KontaDbContext))]
[Migration("20261002100000_AddAccountsAndImportDeduplication")]
public partial class AddAccountsAndImportDeduplication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Accounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                BankAccountNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                BankLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Accounts", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_BankAccountNumber",
            table: "Accounts",
            column: "BankAccountNumber",
            unique: true);

        migrationBuilder.CreateTable(
            name: "ef_temp_Transactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Label = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                AccountNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                AccountLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                AccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                ImportFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                ImportOccurrence = table.Column<int>(type: "INTEGER", nullable: true),
                CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Transactions_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Transactions_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql("INSERT INTO ef_temp_Transactions (Id, Date, Label, Amount, Type, AccountNumber, AccountLabel, CategoryId, Source, CreatedAt, UpdatedAt) SELECT Id, Date, Label, Amount, Type, AccountNumber, AccountLabel, CategoryId, Source, CreatedAt, UpdatedAt FROM Transactions;");
        migrationBuilder.DropTable("Transactions");
        migrationBuilder.RenameTable(name: "ef_temp_Transactions", newName: "Transactions");
        migrationBuilder.CreateIndex("IX_Transactions_CategoryId", "Transactions", "CategoryId");
        migrationBuilder.CreateIndex("IX_Transactions_Date", "Transactions", "Date");
        migrationBuilder.CreateIndex("IX_Transactions_AccountId", "Transactions", "AccountId");
        migrationBuilder.CreateIndex(
            name: "IX_Transactions_AccountId_ImportFingerprint_ImportOccurrence",
            table: "Transactions",
            columns: new[] { "AccountId", "ImportFingerprint", "ImportOccurrence" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Transactions");
        migrationBuilder.CreateTable(
            name: "ef_temp_Transactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Label = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                AccountNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                AccountLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Transactions_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.Sql("INSERT INTO ef_temp_Transactions (Id, Date, Label, Amount, Type, AccountNumber, AccountLabel, CategoryId, Source, CreatedAt, UpdatedAt) SELECT Id, Date, Label, Amount, Type, AccountNumber, AccountLabel, CategoryId, Source, CreatedAt, UpdatedAt FROM Transactions;");
        migrationBuilder.RenameTable(name: "ef_temp_Transactions", newName: "Transactions");
        migrationBuilder.CreateIndex("IX_Transactions_CategoryId", "Transactions", "CategoryId");
        migrationBuilder.CreateIndex("IX_Transactions_Date", "Transactions", "Date");
        migrationBuilder.DropTable("Accounts");
    }
}