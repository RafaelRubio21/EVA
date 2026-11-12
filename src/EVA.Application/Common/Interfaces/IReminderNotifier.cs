using EVA.Domain.Enums;

namespace EVA.Application.Common.Interfaces;

public sealed record ReminderNotificationPayload(
    Guid EmailId,
    string MessageId,
    string Sender,
    string Subject,
    PriorityLevel PriorityLevel,
    string Summary,
    DateTimeOffset? Deadline,
    DateTimeOffset? NextReminderAt);

public interface IReminderNotifier
{
    Task NotifyDueReminderAsync(
        ReminderNotificationPayload payload,
        CancellationToken cancellationToken = default);

    Task NotifyCriticalEmailArrivedAsync(
        ReminderNotificationPayload payload,
        CancellationToken cancellationToken = default);
}
