using EVA.Domain.Entities;
using EVA.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EVA.Infrastructure.Persistence.Repositories;

public sealed class EmailRepository(EvaDbContext dbContext) : IEmailRepository
{
    public async Task<bool> ExistsByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailRecords
            .AsNoTracking()
            .AnyAsync(e => e.MessageId == messageId, cancellationToken);
    }

    public async Task<EmailRecord?> GetByIdWithAnalysisAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailRecords
            .Include(e => e.Analysis)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        EmailRecord emailRecord,
        CancellationToken cancellationToken = default)
    {
        await dbContext.EmailRecords.AddAsync(emailRecord, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailRecord>> GetPendingResponsesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailRecords
            .AsNoTracking()
            .Include(e => e.Analysis)
            .Where(e => e.IsProcessed && e.Analysis != null && e.Analysis.RequiresResponse)
            .OrderByDescending(e => e.Analysis!.PriorityLevel)
            .ThenBy(e => e.Analysis!.Deadline)
            .ThenByDescending(e => e.ReceivedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailRecord>> GetDueRemindersAsync(
        DateTimeOffset referenceTime,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailRecords
            .Include(e => e.Analysis)
            .Where(e =>
                e.IsProcessed &&
                e.Analysis != null &&
                e.Analysis.RequiresResponse &&
                e.Analysis.NextReminderAt != null &&
                e.Analysis.NextReminderAt <= referenceTime)
            .OrderByDescending(e => e.Analysis!.PriorityLevel)
            .ThenBy(e => e.Analysis!.NextReminderAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
