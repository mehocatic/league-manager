using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Reports;

public sealed record RoundReportListItemDto(int Round, DateTime CreatedAtUtc, int SizeBytes);

public sealed record GetRoundReportsQuery : IQuery<IReadOnlyList<RoundReportListItemDto>>;

internal sealed class GetRoundReportsQueryHandler(ILeagueDbContext db) : IQueryHandler<GetRoundReportsQuery, IReadOnlyList<RoundReportListItemDto>>
{
    public async Task<IReadOnlyList<RoundReportListItemDto>> HandleAsync(GetRoundReportsQuery query, CancellationToken cancellationToken) =>
        // Content.Length becomes SQL length(), so listing never loads the PDF bytes.
        await db.RoundReports
            .OrderByDescending(r => r.Round)
            .Select(r => new RoundReportListItemDto(r.Round, r.CreatedAtUtc, r.Content.Length))
            .ToListAsync(cancellationToken);
}

public sealed record GetRoundReportPdfQuery(int Round) : IQuery<Result<byte[]>>;

internal sealed class GetRoundReportPdfQueryHandler(ILeagueDbContext db) : IQueryHandler<GetRoundReportPdfQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> HandleAsync(GetRoundReportPdfQuery query, CancellationToken cancellationToken)
    {
        var content = await db.RoundReports
            .Where(r => r.Round == query.Round)
            .Select(r => r.Content)
            .SingleOrDefaultAsync(cancellationToken);

        return content is null ? Error.NotFound($"There is no report for round {query.Round} yet.") : content;
    }
}
