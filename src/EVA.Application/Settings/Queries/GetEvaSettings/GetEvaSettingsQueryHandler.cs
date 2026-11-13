using EVA.Domain.Entities;
using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Settings.Queries.GetEvaSettings;

public sealed class GetEvaSettingsQueryHandler(IEvaSettingsRepository settingsRepository)
    : IRequestHandler<GetEvaSettingsQuery, EvaSettingsDto>
{
    public async Task<EvaSettingsDto> Handle(
        GetEvaSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetCurrentAsync(cancellationToken)
            ?? EvaSettings.CreateDefault();

        return new EvaSettingsDto(
            settings.EmailPollingIntervalSeconds,
            settings.ReminderCheckIntervalSeconds,
            settings.UpdatedAt);
    }
}
