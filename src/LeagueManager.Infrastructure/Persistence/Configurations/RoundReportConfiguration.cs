using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueManager.Infrastructure.Persistence.Configurations;

public class RoundReportConfiguration : IEntityTypeConfiguration<RoundReport>
{
    public void Configure(EntityTypeBuilder<RoundReport> builder)
    {
        // One report per round: regenerating replaces it.
        builder.HasIndex(r => r.Round).IsUnique();
        builder.Property(r => r.Content).IsRequired();
        builder.Property(r => r.CreatedAtUtc).HasConversion(UtcConverter.Instance);
    }
}
