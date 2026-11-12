using EVA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVA.Infrastructure.Persistence.Configurations;

public sealed class EmailRecordConfiguration : IEntityTypeConfiguration<EmailRecord>
{
    public void Configure(EntityTypeBuilder<EmailRecord> builder)
    {
        builder.ToTable("EmailRecords");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.MessageId)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(e => e.MessageId)
            .IsUnique();

        builder.Property(e => e.Sender)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Body)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.ReceivedAt)
            .IsRequired();

        builder.Property(e => e.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(e => new { e.IsProcessed, e.ReceivedAt });

        builder.HasOne(e => e.Analysis)
            .WithOne()
            .HasForeignKey<EmailAnalysis>(a => a.EmailRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
