using Microsoft.Extensions.Caching.Memory;

namespace LeagueManager.Application.Standings;

// Standings and top scorers are read on every public page view but only change when the admin writes,
// so they are calculated once and served from memory until a command invalidates them.
internal static class StandingsCache
{
    public const string StandingsKey = "standings";
    public const string TopScorersKey = "top-scorers";

    // Safety net only: invalidation keeps the cache correct, but if the database is ever edited outside the app,
    // stale data still disappears within an hour.
    public static readonly MemoryCacheEntryOptions EntryOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };

    public static void InvalidateStandings(this IMemoryCache cache)
    {
        cache.Remove(StandingsKey);
        cache.Remove(TopScorersKey);
    }
}
