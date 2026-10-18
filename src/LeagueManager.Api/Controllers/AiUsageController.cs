using LeagueManager.Application.Abstractions;
using LeagueManager.Application.AiUsage;
using LeagueManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeagueManager.Api.Controllers;

[Route("api/ai-usage")]
[Authorize(Roles = Roles.Admin)]
public sealed class AiUsageController(IQueryHandler<GetAiUsageQuery, AiUsageReportDto> getAiUsage) : ApiController
{
    [HttpGet]
    [ProducesResponseType<AiUsageReportDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await getAiUsage.HandleAsync(new GetAiUsageQuery(), cancellationToken));
}
