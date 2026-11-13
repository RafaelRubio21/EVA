using EVA.Application.Common.Models;
using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Emails.Queries.GetEmails;

public sealed class GetEmailsQueryHandler(IEmailRepository emailRepository)
    : IRequestHandler<GetEmailsQuery, PagedResult<EmailSummaryDto>>
{
    public async Task<PagedResult<EmailSummaryDto>> Handle(
        GetEmailsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await emailRepository.GetPagedAsync(
            request.AccountEmail,
            request.PriorityLevel,
            request.RequiresResponse,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items
            .Select(e => new EmailSummaryDto(
                e.Id,
                e.MessageId,
                e.AccountEmail,
                e.Sender,
                e.Subject,
                e.ReceivedAt,
                e.IsProcessed,
                e.Analysis?.PriorityLevel,
                e.Analysis?.PriorityLevel.ToString(),
                e.Analysis?.RequiresResponse,
                e.Analysis?.Summary,
                e.Analysis?.SuggestedReplyDraft,
                e.Analysis?.Deadline,
                e.Analysis?.NextReminderAt))
            .ToList();

        return new PagedResult<EmailSummaryDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
