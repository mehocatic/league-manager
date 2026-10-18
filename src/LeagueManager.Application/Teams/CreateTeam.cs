using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Standings;
using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LeagueManager.Application.Teams;

public sealed record CreateTeamCommand(string Name, string City) : ICommand<Result<TeamDto>>;

internal sealed class CreateTeamCommandHandler(ILeagueDbContext db, IMemoryCache cache) : ICommandHandler<CreateTeamCommand, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        // Validation lives here, not in the controller, so the same rules apply to any future caller.
        var name = command.Name?.Trim() ?? string.Empty;
        var city = command.City?.Trim() ?? string.Empty;

        if (name.Length is 0 or > Team.NameMaxLength)
        {
            return Error.Validation($"Name is required and must be at most {Team.NameMaxLength} characters.");
        }

        if (city.Length is 0 or > Team.CityMaxLength)
        {
            return Error.Validation($"City is required and must be at most {Team.CityMaxLength} characters.");
        }

        // Checked up front to return a clear 409; the unique index on Name remains the final guarantee.
        if (await db.Teams.AnyAsync(t => t.Name == name, cancellationToken))
        {
            return Error.Conflict($"A team named '{name}' already exists.");
        }

        var team = new Team { Name = name, City = city };
        db.Teams.Add(team);
        await db.SaveChangesAsync(cancellationToken);

        // A new team must appear in the table (with zero points), so the cached table is now outdated.
        cache.InvalidateStandings();

        return new TeamDto(team.Id, team.Name, team.City);
    }
}
