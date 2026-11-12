using EVA.Domain.Entities;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EVA.Application.Emails.Commands.RescheduleEmailReminder;

public sealed class RescheduleEmailReminderCommandHandler(
    IEmailRepository emailRepository,
    ILogger<RescheduleEmailReminderCommandHandler> logger)
    : IRequestHandler<RescheduleEmailReminderCommand, DateTimeOffset>
{
    public async Task<DateTimeOffset> Handle(
        RescheduleEmailReminderCommand request,
        CancellationToken cancellationToken)
    {
        var emailRecord = await emailRepository.GetByIdWithAnalysisAsync(request.EmailId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmailRecord), request.EmailId);

        emailRecord.RescheduleReminder(request.NextReminderAt);

        await emailRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Lembrete do e-mail '{EmailId}' reagendado para {NextReminderAt:O}.",
            emailRecord.Id,
            request.NextReminderAt);

        return emailRecord.Analysis!.NextReminderAt!.Value;
    }
}
