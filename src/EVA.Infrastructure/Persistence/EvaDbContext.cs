using EVA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVA.Infrastructure.Persistence;

public sealed class EvaDbContext(DbContextOptions<EvaDbContext> options) : DbContext(options)
{
    public DbSet<EmailRecord> EmailRecords => Set<EmailRecord>();
    public DbSet<EmailAnalysis> EmailAnalyses => Set<EmailAnalysis>();
    public DbSet<EvaSettings> EvaSettings => Set<EvaSettings>();
    public DbSet<EmailAccount> EmailAccounts => Set<EmailAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EvaDbContext).Assembly);
    }
}
