using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Emails.Queries.GetPendingEmails;

public sealed class GetPendingEmailsQueryHandler(IEmailRepository emailRepository)
    : IRequestHandler<GetPendingEmailsQuery, IReadOnlyList<PendingEmailDto>>
{
    public async Task<IReadOnlyList<PendingEmailDto>> Handle(
        GetPendingEmailsQuery request,
        CancellationToken cancellationToken)
    {
        var pendingEmails = await emailRepository.GetPendingResponsesAsync(cancellationToken);

        return pendingEmails
            .Where(e => e.Analysis is not null)
            .Select(e => new PendingEmailDto(
                e.Id,
                e.MessageId,
                e.AccountEmail,
                e.Sender,
                e.Subject,
                e.ReceivedAt,
                e.Analysis!.PriorityLevel,
                e.Analysis.PriorityLevel.ToString(),
                e.Analysis.Summary,
                e.Analysis.Deadline,
                e.Analysis.NextReminderAt))
            .ToList();
    }
}
