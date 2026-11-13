namespace EVA.Application.Settings.Queries.GetEvaSettings;

public sealed record EvaSettingsDto(
    int EmailPollingIntervalSeconds,
    int ReminderCheckIntervalSeconds,
    DateTimeOffset UpdatedAt);
