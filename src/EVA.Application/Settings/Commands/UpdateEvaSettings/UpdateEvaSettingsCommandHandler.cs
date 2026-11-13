using EVA.Application.Settings.Queries.GetEvaSettings;
using EVA.Domain.Entities;
using EVA.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EVA.Application.Settings.Commands.UpdateEvaSettings;

public sealed class UpdateEvaSettingsCommandHandler(
    IEvaSettingsRepository settingsRepository,
    ILogger<UpdateEvaSettingsCommandHandler> logger)
    : IRequestHandler<UpdateEvaSettingsCommand, EvaSettingsDto>
{
    public async Task<EvaSettingsDto> Handle(
        UpdateEvaSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetCurrentAsync(cancellationToken);

        if (settings is null)
        {
            settings = EvaSettings.CreateDefault();
            settings.UpdateIntervals(
                request.EmailPollingIntervalSeconds,
                request.ReminderCheckIntervalSeconds);
            await settingsRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.UpdateIntervals(
                request.EmailPollingIntervalSeconds,
                request.ReminderCheckIntervalSeconds);
        }

        await settingsRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[EVA Settings] Intervalos atualizados: EmailPolling={EmailSeconds}s, ReminderCheck={ReminderSeconds}s.",
            settings.EmailPollingIntervalSeconds,
            settings.ReminderCheckIntervalSeconds);

        return new EvaSettingsDto(
            settings.EmailPollingIntervalSeconds,
            settings.ReminderCheckIntervalSeconds,
            settings.UpdatedAt);
    }
}
