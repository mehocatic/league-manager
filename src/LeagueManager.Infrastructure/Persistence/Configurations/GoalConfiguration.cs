using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueManager.Infrastructure.Persistence.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.HasOne(g => g.Match)
               .WithMany(m => m.Goals)
               .HasForeignKey(g => g.MatchId)
               .OnDelete(DeleteBehavior.Cascade); 

        builder.HasOne(g => g.Player)
               .WithMany()
               .HasForeignKey(g => g.PlayerId)
               .OnDelete(DeleteBehavior.Restrict);
               
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Goal_Minute", $"\"Minute\" BETWEEN {Goal.MinMinute} AND {Goal.MaxMinute}"));
    }
}