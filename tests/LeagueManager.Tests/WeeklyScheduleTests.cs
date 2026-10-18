using LeagueManager.Application.Reports;

namespace LeagueManager.Tests;

public class WeeklyScheduleTests
{
    private static readonly TimeZoneInfo Sarajevo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");

    private static DateTimeOffset Next(string now) =>
        WeeklySchedule.NextRun(DateTimeOffset.Parse(now), DayOfWeek.Monday, new TimeOnly(8, 0), Sarajevo);

    [Theory]
    // Sunday evening (summer time, +02:00) -> the next morning.
    [InlineData("2026-10-18T20:00:00+02:00", "2026-10-19T08:00:00+02:00")]
    // A minute before the run -> today.
    [InlineData("2026-10-19T07:59:00+02:00", "2026-10-19T08:00:00+02:00")]
    // Exactly at the run time -> next week, which is after the switch to winter time on 25 October (+01:00).
    [InlineData("2026-10-19T08:00:00+02:00", "2026-10-26T08:00:00+01:00")]
    // "Now" given in UTC is converted to Sarajevo time first: 06:30 UTC is 07:30 local.
    [InlineData("2026-10-26T06:30:00+00:00", "2026-10-26T08:00:00+01:00")]
    public void Next_run_is_monday_08_00_sarajevo_time(string now, string expected)
    {
        var next = Next(now);

        Assert.Equal(DateTimeOffset.Parse(expected), next);
        Assert.Equal(DateTimeOffset.Parse(expected).Offset, next.Offset);
    }
}
