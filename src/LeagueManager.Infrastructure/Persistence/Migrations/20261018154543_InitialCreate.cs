using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeagueManager.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261018154543_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Teams",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teams", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Matches",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Round = table.Column<int>(type: "INTEGER", nullable: false),
                KickOff = table.Column<DateTime>(type: "TEXT", nullable: false),
                HomeTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                AwayTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                HomeGoals = table.Column<int>(type: "INTEGER", nullable: true),
                AwayGoals = table.Column<int>(type: "INTEGER", nullable: true),
                Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Matches", x => x.Id);
                table.CheckConstraint("CK_Match_DifferentTeams", "\"HomeTeamId\" <> \"AwayTeamId\"");
                table.CheckConstraint("CK_Match_GoalsNotNegative", "\"HomeGoals\" >= 0 AND \"AwayGoals\" >= 0");
                table.ForeignKey(
                    name: "FK_Matches_Teams_AwayTeamId",
                    column: x => x.AwayTeamId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Matches_Teams_HomeTeamId",
                    column: x => x.HomeTeamId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Players",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FirstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                LastName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                ShirtNumber = table.Column<int>(type: "INTEGER", nullable: false),
                TeamId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Players", x => x.Id);
                table.ForeignKey(
                    name: "FK_Players_Teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Goals",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Minute = table.Column<int>(type: "INTEGER", nullable: false),
                MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Goals", x => x.Id);
                table.CheckConstraint("CK_Goal_Minute", "\"Minute\" BETWEEN 1 AND 130");
                table.ForeignKey(
                    name: "FK_Goals_Matches_MatchId",
                    column: x => x.MatchId,
                    principalTable: "Matches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Goals_Players_PlayerId",
                    column: x => x.PlayerId,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Goals_MatchId",
            table: "Goals",
            column: "MatchId");

        migrationBuilder.CreateIndex(
            name: "IX_Goals_PlayerId",
            table: "Goals",
            column: "PlayerId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_AwayTeamId",
            table: "Matches",
            column: "AwayTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_HomeTeamId",
            table: "Matches",
            column: "HomeTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_Round",
            table: "Matches",
            column: "Round");

        migrationBuilder.CreateIndex(
            name: "IX_Players_TeamId_ShirtNumber",
            table: "Players",
            columns: new[] { "TeamId", "ShirtNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Teams_Name",
            table: "Teams",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Goals");

        migrationBuilder.DropTable(
            name: "Matches");

        migrationBuilder.DropTable(
            name: "Players");

        migrationBuilder.DropTable(
            name: "Teams");
    }
}
