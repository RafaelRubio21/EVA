using MediatR;

namespace EVA.Application.Emails.Queries.GetPendingEmails;

public sealed record GetPendingEmailsQuery : IRequest<IReadOnlyList<PendingEmailDto>>;
