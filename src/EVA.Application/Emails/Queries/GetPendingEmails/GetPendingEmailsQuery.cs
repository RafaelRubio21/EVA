using EVA.Domain.Enums;
using MediatR;

namespace EVA.Application.Emails.Queries.GetPendingEmails;

public sealed record GetPendingEmailsQuery(
    string? AccountEmail = null,
    PriorityLevel? PriorityLevel = null) : IRequest<IReadOnlyList<PendingEmailDto>>;
