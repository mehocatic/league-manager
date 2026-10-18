using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Abstractions;

// Handlers depend on this interface instead of the concrete LeagueDbContext, so Application never references
// Infrastructure. Exposing DbSet (rather than a repository per entity) keeps LINQ projections and EF's
// unit of work available without wrapping them in a second abstraction layer.
public interface ILeagueDbContext
{
    DbSet<Team> Teams { get; }
    DbSet<Player> Players { get; }
    DbSet<Match> Matches { get; }
    DbSet<Goal> Goals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
