using EVA.Application.Common.Interfaces;
using EVA.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EVA.Infrastructure.BackgroundJobs;

public sealed class ReminderNotificationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderNotificationBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[ReminderWorker] Monitor de lembretes da EVA iniciado.");

        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                await CheckDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ReminderWorker] Erro ao verificar lembretes vencidos.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckDueRemindersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var reminderNotifier = scope.ServiceProvider.GetRequiredService<IReminderNotifier>();

        var now = DateTimeOffset.UtcNow;
        var dueEmails = await emailRepository.GetDueRemindersAsync(now, cancellationToken);

        if (dueEmails.Count == 0)
            return;

        foreach (var email in dueEmails)
        {
            if (email.Analysis is null)
                continue;

            email.RegisterReminderSent(now);

            var payload = new ReminderNotificationPayload(
                email.Id,
                email.MessageId,
                email.Sender,
                email.Subject,
                email.Analysis.PriorityLevel,
                email.Analysis.Summary,
                email.Analysis.Deadline,
                email.Analysis.NextReminderAt);

            await reminderNotifier.NotifyDueReminderAsync(payload, cancellationToken);

            logger.LogWarning(
                "[ALERTA EVA] Lembrete emitido para '{Subject}' ({Sender}). Próximo lembrete reagendado (snooze) para {NextReminderAt:O}.",
                email.Subject,
                email.Sender,
                email.Analysis.NextReminderAt);
        }

        await emailRepository.SaveChangesAsync(cancellationToken);
    }
}
