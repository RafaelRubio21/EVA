using EVA.Domain.Entities;

namespace EVA.Domain.Repositories;

public interface IEmailAccountRepository
{
    Task<IReadOnlyList<EmailAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailAccount>> GetActiveAccountsAsync(CancellationToken cancellationToken = default);
    Task<EmailAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(EmailAccount account, CancellationToken cancellationToken = default);
    Task DeleteAsync(EmailAccount account, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
