using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueManager.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.City).IsRequired().HasMaxLength(100);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}