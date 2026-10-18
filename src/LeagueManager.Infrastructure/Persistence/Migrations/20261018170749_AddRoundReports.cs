using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeagueManager.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261018170749_AddRoundReports : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RoundReports",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Round = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Content = table.Column<byte[]>(type: "BLOB", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoundReports", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RoundReports_Round",
            table: "RoundReports",
            column: "Round",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RoundReports");
    }
}
