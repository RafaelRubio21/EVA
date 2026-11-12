using EVA.Domain.Enums;

namespace EVA.Application.Common.Interfaces;

public sealed record AiEmailAnalysisResult(
    PriorityLevel PriorityLevel,
    bool RequiresResponse,
    string Summary,
    DateTimeOffset? Deadline,
    DateTimeOffset? SuggestedReminderAt);

public interface IAiAnalyzer
{
    Task<AiEmailAnalysisResult> AnalyzeEmailAsync(
        string sender,
        string subject,
        string body,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken = default);
}
