using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Konta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyDisplaySections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyDisplaySections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyDisplaySections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyDisplaySectionPosts",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SectionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyDisplaySectionPosts", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_MonthlyDisplaySectionPosts_AnnualBudgetPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "AnnualBudgetPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonthlyDisplaySectionPosts_MonthlyDisplaySections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "MonthlyDisplaySections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyDisplaySectionPosts_SectionId",
                table: "MonthlyDisplaySectionPosts",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyDisplaySections_Name",
                table: "MonthlyDisplaySections",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyDisplaySectionPosts");

            migrationBuilder.DropTable(
                name: "MonthlyDisplaySections");
        }
    }
}
