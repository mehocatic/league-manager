using LeagueManager.Application.Abstractions;
using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Infrastructure.Persistence;

public class LeagueDbContext : DbContext, ILeagueDbContext
{
    public LeagueDbContext(DbContextOptions<LeagueDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AiUsageRecord> AiUsageRecords => Set<AiUsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeagueDbContext).Assembly);
    }
}