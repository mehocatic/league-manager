using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeagueManager.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261018164834_AddAiUsageRecords : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AiUsageRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Feature = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                MatchId = table.Column<int>(type: "INTEGER", nullable: true),
                Success = table.Column<bool>(type: "INTEGER", nullable: false),
                StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                Error = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                PromptTokens = table.Column<int>(type: "INTEGER", nullable: false),
                OutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                EstimatedCostUsd = table.Column<double>(type: "REAL", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiUsageRecords", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AiUsageRecords_CreatedAtUtc",
            table: "AiUsageRecords",
            column: "CreatedAtUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AiUsageRecords");
    }
}
