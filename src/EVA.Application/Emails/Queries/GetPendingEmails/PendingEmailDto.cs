using EVA.Domain.Enums;

namespace EVA.Application.Emails.Queries.GetPendingEmails;

public sealed record PendingEmailDto(
    Guid Id,
    string MessageId,
    string Sender,
    string Subject,
    DateTimeOffset ReceivedAt,
    PriorityLevel PriorityLevel,
    string PriorityLabel,
    string Summary,
    DateTimeOffset? Deadline,
    DateTimeOffset? NextReminderAt);
