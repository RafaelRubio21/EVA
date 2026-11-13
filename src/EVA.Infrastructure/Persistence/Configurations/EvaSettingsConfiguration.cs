using EVA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVA.Infrastructure.Persistence.Configurations;

public sealed class EvaSettingsConfiguration : IEntityTypeConfiguration<EvaSettings>
{
    public void Configure(EntityTypeBuilder<EvaSettings> builder)
    {
        builder.ToTable("EvaSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.EmailPollingIntervalSeconds)
            .IsRequired();

        builder.Property(s => s.ReminderCheckIntervalSeconds)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();
    }
}
