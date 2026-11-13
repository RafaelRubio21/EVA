using EVA.Domain.Enums;

namespace EVA.Application.Emails.Queries.GetEmailById;

public sealed record EmailDetailsDto(
    Guid Id,
    string MessageId,
    string AccountEmail,
    string Sender,
    string Subject,
    string Body,
    DateTimeOffset ReceivedAt,
    bool IsProcessed,
    PriorityLevel? PriorityLevel,
    string? PriorityLabel,
    bool? RequiresResponse,
    string? Summary,
    string? SuggestedReplyDraft,
    DateTimeOffset? Deadline,
    DateTimeOffset? NextReminderAt);
