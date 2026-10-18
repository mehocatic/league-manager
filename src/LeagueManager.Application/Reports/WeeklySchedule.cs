namespace LeagueManager.Application.Reports;

public static class WeeklySchedule
{
    // Pure function of "now", so the schedule (including the summer/winter time switch) is unit tested without waiting.
    public static DateTimeOffset NextRun(DateTimeOffset now, DayOfWeek day, TimeOnly time, TimeZoneInfo zone)
    {
        var localNow = TimeZoneInfo.ConvertTime(now, zone);
        var daysUntil = ((int)day - (int)localNow.DayOfWeek + 7) % 7;
        var next = DateOnly.FromDateTime(localNow.DateTime).AddDays(daysUntil).ToDateTime(time);

        if (next <= localNow.DateTime)
        {
            next = next.AddDays(7);
        }

        // The offset is taken for the target date, not for now: a run after the clock change gets the new offset.
        return new DateTimeOffset(next, zone.GetUtcOffset(next));
    }
}
