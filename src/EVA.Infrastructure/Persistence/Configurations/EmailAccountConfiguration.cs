using EVA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVA.Infrastructure.Persistence.Configurations;

public sealed class EmailAccountConfiguration : IEntityTypeConfiguration<EmailAccount>
{
    public void Configure(EntityTypeBuilder<EmailAccount> builder)
    {
        builder.ToTable("EmailAccounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(a => a.Email)
            .IsUnique();

        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.ProviderType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.LastSyncedAt)
            .IsRequired(false);
    }
}
