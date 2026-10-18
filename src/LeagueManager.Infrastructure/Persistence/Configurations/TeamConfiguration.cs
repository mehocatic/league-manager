using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueManager.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(Team.NameMaxLength);
        builder.Property(t => t.City).IsRequired().HasMaxLength(Team.CityMaxLength);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}