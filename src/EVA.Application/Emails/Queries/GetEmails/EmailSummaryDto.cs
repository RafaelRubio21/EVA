using EVA.Domain.Enums;

namespace EVA.Application.Emails.Queries.GetEmails;

public sealed record EmailSummaryDto(
    Guid Id,
    string MessageId,
    string AccountEmail,
    string Sender,
    string Subject,
    DateTimeOffset ReceivedAt,
    bool IsProcessed,
    PriorityLevel? PriorityLevel,
    string? PriorityLabel,
    bool? RequiresResponse,
    string? Summary,
    string? SuggestedReplyDraft,
    DateTimeOffset? Deadline,
    DateTimeOffset? NextReminderAt);
