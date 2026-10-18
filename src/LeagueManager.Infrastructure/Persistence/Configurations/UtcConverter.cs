using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LeagueManager.Infrastructure.Persistence.Configurations;

// SQLite stores dates as text without a time zone, so EF reads them back as DateTimeKind.Unspecified and JSON loses the "Z".
// Only for *Utc columns: KickOff is deliberately local league time.
internal static class UtcConverter
{
    public static readonly ValueConverter<DateTime, DateTime> Instance =
        new(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
