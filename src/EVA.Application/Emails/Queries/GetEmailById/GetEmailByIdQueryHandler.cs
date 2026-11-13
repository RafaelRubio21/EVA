using EVA.Domain.Entities;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Emails.Queries.GetEmailById;

public sealed class GetEmailByIdQueryHandler(IEmailRepository emailRepository)
    : IRequestHandler<GetEmailByIdQuery, EmailDetailsDto>
{
    public async Task<EmailDetailsDto> Handle(
        GetEmailByIdQuery request,
        CancellationToken cancellationToken)
    {
        var email = await emailRepository.GetByIdWithAnalysisAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmailRecord), request.Id);

        return new EmailDetailsDto(
            email.Id,
            email.MessageId,
            email.AccountEmail,
            email.Sender,
            email.Subject,
            email.Body,
            email.ReceivedAt,
            email.IsProcessed,
            email.Analysis?.PriorityLevel,
            email.Analysis?.PriorityLevel.ToString(),
            email.Analysis?.RequiresResponse,
            email.Analysis?.Summary,
            email.Analysis?.SuggestedReplyDraft,
            email.Analysis?.Deadline,
            email.Analysis?.NextReminderAt);
    }
}
