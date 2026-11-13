using EVA.Domain.Entities;
using EVA.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EVA.Infrastructure.Persistence.Repositories;

public sealed class EvaSettingsRepository(EvaDbContext dbContext) : IEvaSettingsRepository
{
    public async Task<EvaSettings?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.EvaSettings
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(EvaSettings settings, CancellationToken cancellationToken = default)
    {
        await dbContext.EvaSettings.AddAsync(settings, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
