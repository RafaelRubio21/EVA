using EVA.Application.Common.Interfaces;
using EVA.Domain.Entities;
using EVA.Domain.Enums;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EVA.Application.Emails.Commands.ProcessNewEmail;

public sealed class ProcessNewEmailCommandHandler(
    IEmailRepository emailRepository,
    IAiAnalyzer aiAnalyzer,
    IEmailProvider emailProvider,
    IReminderNotifier reminderNotifier,
    ILogger<ProcessNewEmailCommandHandler> logger)
    : IRequestHandler<ProcessNewEmailCommand, ProcessNewEmailResult>
{
    public async Task<ProcessNewEmailResult> Handle(
        ProcessNewEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (await emailRepository.ExistsByMessageIdAsync(request.MessageId, cancellationToken))
        {
            throw new DomainException($"O e-mail com MessageId '{request.MessageId}' já foi triado pela EVA.");
        }

        // 1. Instancia a entidade rica de domínio
        var emailRecord = EmailRecord.Create(
            request.MessageId,
            request.AccountEmail,
            request.Sender,
            request.Subject,
            request.Body,
            request.ReceivedAt);

        // 2. Aciona o motor de IA para classificação de prioridade, resumo e prazos
        logger.LogInformation(
            "Analisando e-mail '{MessageId}' de '{Sender}' via IA...",
            emailRecord.MessageId,
            emailRecord.Sender);

        var aiResult = await aiAnalyzer.AnalyzeEmailAsync(
            emailRecord.Sender,
            emailRecord.Subject,
            emailRecord.Body,
            emailRecord.ReceivedAt,
            cancellationToken);

        // 3. Cria a entidade de domínio EmailAnalysis (aplicando invariantes e cálculo de lembrete)
        var analysis = EmailAnalysis.Create(
            emailRecord.Id,
            aiResult.PriorityLevel,
            aiResult.RequiresResponse,
            aiResult.Summary,
            aiResult.Deadline,
            aiResult.SuggestedReminderAt);

        // 4. Anexa a análise ao agregado EmailRecord e marca como processado
        emailRecord.AttachAnalysis(analysis);

        // 5. Persiste no banco de dados e marca como lido no provedor de origem
        await emailRepository.AddAsync(emailRecord, cancellationToken);
        await emailRepository.SaveChangesAsync(cancellationToken);
        await emailProvider.MarkAsReadAsync(emailRecord.MessageId, cancellationToken);

        // 6. Dispara notificação em tempo real caso seja um e-mail urgente/alta prioridade que exige resposta
        if (analysis.RequiresResponse && analysis.PriorityLevel >= PriorityLevel.High)
        {
            await reminderNotifier.NotifyCriticalEmailArrivedAsync(
                new ReminderNotificationPayload(
                    emailRecord.Id,
                    emailRecord.MessageId,
                    emailRecord.Sender,
                    emailRecord.Subject,
                    analysis.PriorityLevel,
                    analysis.Summary,
                    analysis.Deadline,
                    analysis.NextReminderAt),
                cancellationToken);
        }

        return new ProcessNewEmailResult(
            emailRecord.Id,
            analysis.PriorityLevel,
            analysis.RequiresResponse,
            analysis.Summary,
            analysis.Deadline,
            analysis.NextReminderAt);
    }
}
