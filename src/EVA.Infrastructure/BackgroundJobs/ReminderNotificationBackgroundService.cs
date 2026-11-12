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

        var now = DateTimeOffset.UtcNow;
        var dueEmails = await emailRepository.GetDueRemindersAsync(now, cancellationToken);

        foreach (var email in dueEmails)
        {
            logger.LogWarning(
                "[ALERTA EVA] Lembrete de resposta pendente! E-mail '{Subject}' de '{Sender}' (Prioridade: {Priority}, Prazo: {Deadline:O}).",
                email.Subject,
                email.Sender,
                email.Analysis?.PriorityLevel,
                email.Analysis?.Deadline);
        }
    }
}
