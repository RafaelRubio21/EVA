using EVA.Application.Settings.Queries.GetEvaSettings;
using MediatR;

namespace EVA.Application.Settings.Commands.UpdateEvaSettings;

public sealed record UpdateEvaSettingsCommand(
    int EmailPollingIntervalSeconds,
    int ReminderCheckIntervalSeconds) : IRequest<EvaSettingsDto>;
