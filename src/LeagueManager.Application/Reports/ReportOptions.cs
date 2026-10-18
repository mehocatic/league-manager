namespace LeagueManager.Application.Reports;

public sealed class ReportOptions
{
    public const string SectionName = "Reports";

    public string LeagueName { get; set; } = "2. liga FBiH Zapad";
    public bool WeeklyJobEnabled { get; set; } = true;
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;
    public TimeOnly Time { get; set; } = new(8, 0);

    // The schedule and the "generated at" time follow the league's local time, including summer/winter time.
    public string TimeZone { get; set; } = "Europe/Sarajevo";
}
