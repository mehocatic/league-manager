using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueManager.Infrastructure.Persistence.Configurations;

public class AiUsageRecordConfiguration : IEntityTypeConfiguration<AiUsageRecord>
{
    public void Configure(EntityTypeBuilder<AiUsageRecord> builder)
    {
        builder.Property(r => r.Feature).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Model).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Error).HasMaxLength(500);

        builder.Property(r => r.CreatedAtUtc).HasConversion(UtcConverter.Instance);
        builder.HasIndex(r => r.CreatedAtUtc);
    }
}
