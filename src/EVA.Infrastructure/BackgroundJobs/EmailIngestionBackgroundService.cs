using EVA.Application.Common.Interfaces;
using EVA.Application.Emails.Commands.ProcessNewEmail;
using EVA.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EVA.Infrastructure.BackgroundJobs;

public sealed class EmailIngestionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailIngestionBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[EmailIngestionWorker] Serviço de triagem automática iniciado.");

        using var timer = new PeriodicTimer(PollingInterval);

        do
        {
            try
            {
                await IngestUnreadEmailsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[EmailIngestionWorker] Erro durante o ciclo de ingestão de e-mails.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task IngestUnreadEmailsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var unreadEmails = await emailProvider.FetchUnreadEmailsAsync(cancellationToken);

        foreach (var rawEmail in unreadEmails)
        {
            if (await emailRepository.ExistsByMessageIdAsync(rawEmail.MessageId, cancellationToken))
                continue;

            var command = new ProcessNewEmailCommand(
                rawEmail.MessageId,
                rawEmail.AccountEmail,
                rawEmail.Sender,
                rawEmail.Subject,
                rawEmail.Body,
                rawEmail.ReceivedAt);

            var result = await sender.Send(command, cancellationToken);

            logger.LogInformation(
                "[EmailIngestionWorker] E-mail '{MessageId}' triado automaticamente com prioridade {Priority}.",
                rawEmail.MessageId,
                result.PriorityLevel);
        }
    }
}
