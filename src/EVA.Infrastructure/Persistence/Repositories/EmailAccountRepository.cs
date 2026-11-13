using EVA.Domain.Entities;
using EVA.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EVA.Infrastructure.Persistence.Repositories;

public sealed class EmailAccountRepository(EvaDbContext dbContext) : IEmailAccountRepository
{
    public async Task<IReadOnlyList<EmailAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailAccounts
            .AsNoTracking()
            .OrderBy(a => a.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailAccount>> GetActiveAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailAccounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await dbContext.EmailAccounts
            .AsNoTracking()
            .AnyAsync(a => a.Email == normalized, cancellationToken);
    }

    public async Task AddAsync(EmailAccount account, CancellationToken cancellationToken = default)
    {
        await dbContext.EmailAccounts.AddAsync(account, cancellationToken);
    }

    public Task DeleteAsync(EmailAccount account, CancellationToken cancellationToken = default)
    {
        dbContext.EmailAccounts.Remove(account);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
