using System.Globalization;
using LeagueManager.Application.Matches;
using LeagueManager.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LeagueManager.Infrastructure.Reports;

// Labels are Bosnian because the PDF is published for the league's fans, like the AI summaries.
internal sealed class QuestPdfRoundReportRenderer : IRoundReportRenderer
{
    private const string DateTimeFormat = "dd.MM.yyyy. HH:mm";

    static QuestPdfRoundReportRenderer()
    {
        // Free for individuals and organisations under 1M USD annual revenue, which keeps the stack at $0.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(RoundReportData report) =>
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontSize(10));

            page.Header().Column(header =>
            {
                header.Item().Text(report.LeagueName).FontSize(16).Bold();
                header.Item().Text($"Izvještaj {report.Round}. kola").FontSize(13);
                header.Item().Text($"Generisano {Format(report.GeneratedAt.DateTime)}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(10).Column(content =>
            {
                content.Spacing(14);

                content.Item().Text($"Rezultati {report.Round}. kola").FontSize(12).Bold();

                // ShowEntire moves a block that does not fit to the next page instead of splitting it,
                // so a match or the table never starts at the bottom of a page.
                foreach (var match in report.Matches)
                {
                    content.Item().ShowEntire().Element(container => ComposeMatch(container, match));
                }

                content.Item().ShowEntire().Column(section =>
                {
                    section.Spacing(6);
                    section.Item().Text("Tabela").FontSize(12).Bold();
                    section.Item().Element(container => ComposeStandings(container, report));
                });

                content.Item().ShowEntire().Column(section =>
                {
                    section.Spacing(6);
                    section.Item().Text("Najbolji strijelci").FontSize(12).Bold();
                    section.Item().Element(container => ComposeTopScorers(container, report));
                });
            });

            page.Footer().Row(footer =>
            {
                footer.RelativeItem().Text("Automatski generisano: League Manager").FontSize(8).FontColor(Colors.Grey.Darken1);
                footer.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                    text.Span("Stranica ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        })).GeneratePdf();

    private static void ComposeMatch(IContainer container, MatchDetailsDto match)
    {
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingBottom(6).Column(column =>
        {
            column.Spacing(3);

            column.Item().Row(row =>
            {
                row.RelativeItem().AlignRight().Text(match.HomeTeam).Bold();
                row.ConstantItem(60).AlignCenter().Text(match.IsPlayed ? $"{match.HomeGoals}:{match.AwayGoals}" : "-:-").Bold();
                row.RelativeItem().Text(match.AwayTeam).Bold();
            });

            if (!match.IsPlayed)
            {
                column.Item().AlignCenter().Text($"{Format(match.KickOff)}, nije odigrano").FontSize(8).FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Row(row =>
            {
                row.RelativeItem().AlignRight().Text(Scorers(match, match.HomeTeamId)).FontSize(8).FontColor(Colors.Grey.Darken2);
                row.ConstantItem(60);
                row.RelativeItem().Text(Scorers(match, match.AwayTeamId)).FontSize(8).FontColor(Colors.Grey.Darken2);
            });

            if (match.Summary is not null)
            {
                column.Item().PaddingTop(2).Text(match.Summary).FontSize(9).Italic();
            }
        });
    }

    private static void ComposeStandings(IContainer container, RoundReportData report)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(22);
                columns.RelativeColumn();
                for (var i = 0; i < 8; i++)
                {
                    columns.ConstantColumn(30);
                }
            });

            table.Header(header =>
            {
                foreach (var label in new[] { "#", "Klub", "Uk", "Pob", "Ner", "Por", "G+", "G-", "GR", "Bod" })
                {
                    header.Cell().Element(HeaderCell).Text(label).Bold();
                }
            });

            foreach (var row in report.Standings)
            {
                table.Cell().Element(Cell).Text($"{row.Position}.");
                table.Cell().Element(Cell).Text(row.Team);
                foreach (var value in new[] { row.Played, row.Won, row.Drawn, row.Lost, row.GoalsFor, row.GoalsAgainst })
                {
                    table.Cell().Element(Cell).AlignRight().Text(value.ToString(CultureInfo.InvariantCulture));
                }

                table.Cell().Element(Cell).AlignRight().Text(row.GoalDifference > 0 ? $"+{row.GoalDifference}" : $"{row.GoalDifference}");
                table.Cell().Element(Cell).AlignRight().Text(row.Points.ToString(CultureInfo.InvariantCulture)).Bold();
            }
        });
    }

    private static void ComposeTopScorers(IContainer container, RoundReportData report)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(22);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(40);
            });

            table.Header(header =>
            {
                foreach (var label in new[] { "#", "Igrač", "Klub", "Golovi" })
                {
                    header.Cell().Element(HeaderCell).Text(label).Bold();
                }
            });

            foreach (var scorer in report.TopScorers)
            {
                table.Cell().Element(Cell).Text($"{scorer.Rank}.");
                table.Cell().Element(Cell).Text(scorer.Player);
                table.Cell().Element(Cell).Text(scorer.Team);
                table.Cell().Element(Cell).AlignRight().Text(scorer.Goals.ToString(CultureInfo.InvariantCulture)).Bold();
            }
        });
    }

    private static string Scorers(MatchDetailsDto match, int teamId) =>
        string.Join(", ", match.Goals.Where(g => g.TeamId == teamId).Select(g => $"{g.Minute}' {g.Player}"));

    private static string Format(DateTime value) => value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3);

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3);
}
