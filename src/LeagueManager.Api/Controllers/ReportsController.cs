using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Reports;
using LeagueManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeagueManager.Api.Controllers;

[Route("api/reports")]
public sealed class ReportsController(
    IQueryHandler<GetRoundReportsQuery, IReadOnlyList<RoundReportListItemDto>> getReports,
    IQueryHandler<GetRoundReportPdfQuery, Result<byte[]>> getReportPdf,
    ICommandHandler<GenerateRoundReportCommand, Result<RoundReportInfoDto>> generateReport) : ApiController
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoundReportListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await getReports.HandleAsync(new GetRoundReportsQuery(), cancellationToken));

    [HttpGet("rounds/{round:int}")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(int round, CancellationToken cancellationToken)
    {
        var result = await getReportPdf.HandleAsync(new GetRoundReportPdfQuery(round), cancellationToken);
        return result.Match<IActionResult>(pdf => File(pdf, "application/pdf", $"izvjestaj-{round}-kolo.pdf"), ErrorResult);
    }

    // Runs the same work as the Monday job immediately, e.g. after correcting a result. Without round: the latest played round.
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<RoundReportInfoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Generate([FromQuery] int? round, CancellationToken cancellationToken)
    {
        var result = await generateReport.HandleAsync(new GenerateRoundReportCommand(round), cancellationToken);
        return result.Match<IActionResult>(
            info => CreatedAtAction(nameof(GetPdf), new { round = info.Round }, info),
            ErrorResult);
    }
}
