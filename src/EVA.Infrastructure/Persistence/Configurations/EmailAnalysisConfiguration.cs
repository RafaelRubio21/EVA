using EVA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVA.Infrastructure.Persistence.Configurations;

public sealed class EmailAnalysisConfiguration : IEntityTypeConfiguration<EmailAnalysis>
{
    public void Configure(EntityTypeBuilder<EmailAnalysis> builder)
    {
        builder.ToTable("EmailAnalyses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.EmailRecordId)
            .IsRequired();

        builder.HasIndex(a => a.EmailRecordId)
            .IsUnique();

        builder.Property(a => a.PriorityLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.RequiresResponse)
            .IsRequired();

        builder.Property(a => a.Summary)
            .IsRequired()
            .HasMaxLength(1500);

        builder.Property(a => a.SuggestedReplyDraft)
            .IsRequired(false)
            .HasMaxLength(4000);

        builder.Property(a => a.Deadline)
            .IsRequired(false);

        builder.Property(a => a.NextReminderAt)
            .IsRequired(false);

        builder.HasIndex(a => new { a.RequiresResponse, a.PriorityLevel, a.NextReminderAt });
    }
}
