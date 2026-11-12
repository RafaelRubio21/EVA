using EVA.Domain.Entities;

namespace EVA.Domain.Repositories;

public interface IEmailRepository
{
    Task<bool> ExistsByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);
    Task<EmailRecord?> GetByIdWithAnalysisAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(EmailRecord emailRecord, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailRecord>> GetPendingResponsesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailRecord>> GetDueRemindersAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
