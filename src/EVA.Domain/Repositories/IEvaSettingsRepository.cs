using EVA.Domain.Entities;

namespace EVA.Domain.Repositories;

public interface IEvaSettingsRepository
{
    Task<EvaSettings?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task AddAsync(EvaSettings settings, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
