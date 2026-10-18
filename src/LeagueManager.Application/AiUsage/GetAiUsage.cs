using LeagueManager.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.AiUsage;

public sealed record AiUsageEntryDto(
    DateTime CreatedAtUtc, string Feature, string Model, int? MatchId, bool Success, string? Error,
    int PromptTokens, int OutputTokens, int DurationMs, double EstimatedCostUsd);

public sealed record AiUsageReportDto(
    int Calls, int FailedCalls, long PromptTokens, long OutputTokens, double EstimatedCostUsd, IReadOnlyList<AiUsageEntryDto> Recent);

public sealed record GetAiUsageQuery : IQuery<AiUsageReportDto>;

internal sealed class GetAiUsageQueryHandler(ILeagueDbContext db) : IQueryHandler<GetAiUsageQuery, AiUsageReportDto>
{
    private const int RecentCount = 20;

    public async Task<AiUsageReportDto> HandleAsync(GetAiUsageQuery query, CancellationToken cancellationToken)
    {
        // Grouping by a constant lets SQL compute all totals in one query instead of loading every row.
        var totals = await db.AiUsageRecords
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Calls = g.Count(),
                FailedCalls = g.Count(r => !r.Success),
                PromptTokens = g.Sum(r => (long)r.PromptTokens),
                OutputTokens = g.Sum(r => (long)r.OutputTokens),
                Cost = g.Sum(r => r.EstimatedCostUsd)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var recent = await db.AiUsageRecords
            .OrderByDescending(r => r.Id)
            .Take(RecentCount)
            .Select(r => new AiUsageEntryDto(
                r.CreatedAtUtc, r.Feature, r.Model, r.MatchId, r.Success, r.Error,
                r.PromptTokens, r.OutputTokens, r.DurationMs, r.EstimatedCostUsd))
            .ToListAsync(cancellationToken);

        return totals is null
            ? new AiUsageReportDto(0, 0, 0, 0, 0, recent)
            : new AiUsageReportDto(totals.Calls, totals.FailedCalls, totals.PromptTokens, totals.OutputTokens, totals.Cost, recent);
    }
}
