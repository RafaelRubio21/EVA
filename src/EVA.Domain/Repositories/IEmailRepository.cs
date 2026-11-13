using EVA.Domain.Entities;
using EVA.Domain.Enums;

namespace EVA.Domain.Repositories;

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
    Task<IReadOnlyList<EmailRecord>> GetDueRemindersAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
