using EVA.Domain.Entities;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EVA.Application.Emails.Commands.CompleteEmailResponse;

public sealed class CompleteEmailResponseCommandHandler(
    IEmailRepository emailRepository,
    ILogger<CompleteEmailResponseCommandHandler> logger)
    : IRequestHandler<CompleteEmailResponseCommand>
{
    public async Task Handle(
        CompleteEmailResponseCommand request,
        CancellationToken cancellationToken)
    {
        var emailRecord = await emailRepository.GetByIdWithAnalysisAsync(request.EmailId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmailRecord), request.EmailId);

        emailRecord.CompleteResponse();

        await emailRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Resposta do e-mail '{EmailId}' ({MessageId}) marcada como concluída.",
            emailRecord.Id,
            emailRecord.MessageId);
    }
}
