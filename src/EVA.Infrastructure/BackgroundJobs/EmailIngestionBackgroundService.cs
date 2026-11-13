using EVA.Application.Common.Interfaces;
using EVA.Application.Emails.Commands.ProcessNewEmail;
using EVA.Domain.Entities;
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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[EmailIngestionWorker] Serviço de triagem automática iniciado.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(EvaSettings.DefaultEmailPollingSeconds));

        do
        {
            try
            {
                var nextInterval = await IngestUnreadEmailsAsync(stoppingToken);
                if (timer.Period != nextInterval)
                {
                    timer.Period = nextInterval;
                    logger.LogInformation(
                        "[EmailIngestionWorker] Intervalo de leitura ajustado para {IntervalSeconds}s.",
                        nextInterval.TotalSeconds);
                }
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

    private async Task<TimeSpan> IngestUnreadEmailsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var accountRepository = scope.ServiceProvider.GetRequiredService<IEmailAccountRepository>();
        var settingsRepository = scope.ServiceProvider.GetRequiredService<IEvaSettingsRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var settings = await settingsRepository.GetCurrentAsync(cancellationToken);
        var intervalSeconds = settings?.EmailPollingIntervalSeconds ?? EvaSettings.DefaultEmailPollingSeconds;

        var activeAccounts = await accountRepository.GetActiveAccountsAsync(cancellationToken);

        // Se ainda não houver contas cadastradas na tabela EmailAccounts, usa o provedor padrão (retrocompatibilidade)
        if (activeAccounts.Count == 0)
        {
            await ProcessAccountInboxAsync(null, emailProvider, emailRepository, sender, cancellationToken);
        }
        else
        {
            foreach (var account in activeAccounts)
            {
                try
                {
                    await ProcessAccountInboxAsync(account.Email, emailProvider, emailRepository, sender, cancellationToken);
                    account.MarkSynced(DateTimeOffset.UtcNow);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[EmailIngestionWorker] Falha ao sincronizar conta '{Email}'.", account.Email);
                }
            }

            await accountRepository.SaveChangesAsync(cancellationToken);
        }

        return TimeSpan.FromSeconds(intervalSeconds);
    }

    private async Task ProcessAccountInboxAsync(
        string? accountEmail,
        IEmailProvider emailProvider,
        IEmailRepository emailRepository,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var unreadEmails = await emailProvider.FetchUnreadEmailsAsync(accountEmail, cancellationToken);

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
                "[EmailIngestionWorker] E-mail '{MessageId}' ({Account}) triado automaticamente com prioridade {Priority}.",
                rawEmail.MessageId,
                rawEmail.AccountEmail,
                result.PriorityLevel);
        }
    }
}
