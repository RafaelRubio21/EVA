using EVA.Domain.Entities;
using EVA.Domain.Enums;

namespace EVA.Domain.Repositories;

public sealed record AccountEmailStats(string AccountEmail, int TotalEmails, int PendingResponses);

public sealed record EmailStatsSummary(
    int TotalEmails,
    int PendingResponses,
    int OverdueDeadlines,
    int CriticalPending,
    int HighPending,
    int NormalPending,
    int LowPending,
    IReadOnlyList<AccountEmailStats> ByAccount);

public interface IEmailRepository
{
    Task<bool> ExistsByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);
    Task<EmailRecord?> GetByIdWithAnalysisAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(EmailRecord emailRecord, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailRecord>> GetPendingResponsesAsync(
        string? accountEmail = null,
        PriorityLevel? priorityLevel = null,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<EmailRecord> Items, int TotalCount)> GetPagedAsync(
        string? accountEmail,
        PriorityLevel? priorityLevel,
        bool? requiresResponse,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<EmailStatsSummary> GetStatsAsync(
        string? accountEmail,
        DateTimeOffset referenceTime,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailRecord>> GetDueRemindersAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
