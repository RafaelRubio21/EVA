using EVA.Domain.Enums;
using MediatR;

namespace EVA.Application.Emails.Commands.ProcessNewEmail;

public sealed record ProcessNewEmailCommand(
    string MessageId,
    string Sender,
    string Subject,
    string Body,
    DateTimeOffset ReceivedAt) : IRequest<ProcessNewEmailResult>;

public sealed record ProcessNewEmailResult(
    Guid EmailRecordId,
    PriorityLevel PriorityLevel,
    bool RequiresResponse,
    string Summary,
    DateTimeOffset? Deadline,
    DateTimeOffset? NextReminderAt);
