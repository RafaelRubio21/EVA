using MediatR;

namespace EVA.Application.Emails.Commands.RescheduleEmailReminder;

public sealed record RescheduleEmailReminderCommand(
    Guid EmailId,
    DateTimeOffset NextReminderAt) : IRequest<DateTimeOffset>;
