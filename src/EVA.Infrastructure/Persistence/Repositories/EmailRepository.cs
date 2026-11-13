using EVA.Domain.Entities;
using EVA.Domain.Enums;
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
        string? accountEmail = null,
        PriorityLevel? priorityLevel = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmailRecords
            .AsNoTracking()
            .Include(e => e.Analysis)
            .Where(e => e.IsProcessed && e.Analysis != null && e.Analysis.RequiresResponse);

        if (!string.IsNullOrWhiteSpace(accountEmail))
        {
            var normalizedAccount = accountEmail.Trim().ToLowerInvariant();
            query = query.Where(e => e.AccountEmail == normalizedAccount);
        }

        if (priorityLevel.HasValue)
        {
            query = query.Where(e => e.Analysis!.PriorityLevel == priorityLevel.Value);
        }

        return await query
            .OrderByDescending(e => e.Analysis!.PriorityLevel)
            .ThenBy(e => e.Analysis!.Deadline)
            .ThenByDescending(e => e.ReceivedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<EmailRecord> Items, int TotalCount)> GetPagedAsync(
        string? accountEmail,
        PriorityLevel? priorityLevel,
        bool? requiresResponse,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmailRecords
            .AsNoTracking()
            .Include(e => e.Analysis)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(accountEmail))
        {
            var normalizedAccount = accountEmail.Trim().ToLowerInvariant();
            query = query.Where(e => e.AccountEmail == normalizedAccount);
        }

        if (priorityLevel.HasValue)
        {
            query = query.Where(e => e.Analysis != null && e.Analysis.PriorityLevel == priorityLevel.Value);
        }

        if (requiresResponse.HasValue)
        {
            query = query.Where(e => e.Analysis != null && e.Analysis.RequiresResponse == requiresResponse.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EmailStatsSummary> GetStatsAsync(
        string? accountEmail,
        DateTimeOffset referenceTime,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmailRecords
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(accountEmail))
        {
            var normalizedAccount = accountEmail.Trim().ToLowerInvariant();
            query = query.Where(e => e.AccountEmail == normalizedAccount);
        }

        var totalEmails = await query.CountAsync(cancellationToken);

        var pendingQuery = query.Where(e => e.IsProcessed && e.Analysis != null && e.Analysis.RequiresResponse);

        var pendingResponses = await pendingQuery.CountAsync(cancellationToken);
        var overdueDeadlines = await pendingQuery.CountAsync(
            e => e.Analysis!.Deadline != null && e.Analysis.Deadline < referenceTime,
            cancellationToken);

        var priorityCounts = await pendingQuery
            .GroupBy(e => e.Analysis!.PriorityLevel)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byAccount = await query
            .GroupBy(e => e.AccountEmail)
            .Select(g => new AccountEmailStats(
                g.Key,
                g.Count(),
                g.Count(e => e.IsProcessed && e.Analysis != null && e.Analysis.RequiresResponse)))
            .OrderBy(a => a.AccountEmail)
            .ToListAsync(cancellationToken);

        int GetCount(PriorityLevel level) =>
            priorityCounts.FirstOrDefault(p => p.Priority == level)?.Count ?? 0;

        return new EmailStatsSummary(
            totalEmails,
            pendingResponses,
            overdueDeadlines,
            GetCount(PriorityLevel.Critical),
            GetCount(PriorityLevel.High),
            GetCount(PriorityLevel.Normal),
            GetCount(PriorityLevel.Low),
            byAccount);
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
