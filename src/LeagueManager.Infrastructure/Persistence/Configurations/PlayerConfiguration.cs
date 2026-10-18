using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueManager.Infrastructure.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(50);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(50);

        builder.HasOne(p => p.Team)
               .WithMany(t => t.Players)
               .HasForeignKey(p => p.TeamId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TeamId, p.ShirtNumber }).IsUnique();
    }
}