using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueManager.Infrastructure.Persistence.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasOne(m => m.HomeTeam)
               .WithMany()
               .HasForeignKey(m => m.HomeTeamId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.AwayTeam)
               .WithMany()
               .HasForeignKey(m => m.AwayTeamId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.Summary).HasMaxLength(2000);

        builder.HasIndex(m => m.Round);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Match_DifferentTeams", "\"HomeTeamId\" <> \"AwayTeamId\"");
            t.HasCheckConstraint("CK_Match_GoalsNotNegative", "\"HomeGoals\" >= 0 AND \"AwayGoals\" >= 0");
        });
    }
}