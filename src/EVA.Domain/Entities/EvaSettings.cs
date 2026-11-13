using EVA.Domain.Common;
using EVA.Domain.Exceptions;

namespace EVA.Domain.Entities;

public sealed class EvaSettings : Entity
{
    public const int MinIntervalSeconds = 15;
    public const int MaxIntervalSeconds = 86400;
    public const int DefaultEmailPollingSeconds = 120;
    public const int DefaultReminderCheckSeconds = 60;

    public int EmailPollingIntervalSeconds { get; private set; }
    public int ReminderCheckIntervalSeconds { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private EvaSettings() { }

    private EvaSettings(int emailPollingIntervalSeconds, int reminderCheckIntervalSeconds)
    {
        UpdateIntervals(emailPollingIntervalSeconds, reminderCheckIntervalSeconds);
    }

    public static EvaSettings CreateDefault()
        => new(DefaultEmailPollingSeconds, DefaultReminderCheckSeconds);

    public void UpdateIntervals(int emailPollingIntervalSeconds, int reminderCheckIntervalSeconds)
    {
        ValidateInterval(emailPollingIntervalSeconds, nameof(EmailPollingIntervalSeconds));
        ValidateInterval(reminderCheckIntervalSeconds, nameof(ReminderCheckIntervalSeconds));

        EmailPollingIntervalSeconds = emailPollingIntervalSeconds;
        ReminderCheckIntervalSeconds = reminderCheckIntervalSeconds;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateInterval(int seconds, string fieldName)
    {
        if (seconds < MinIntervalSeconds || seconds > MaxIntervalSeconds)
        {
            throw new DomainException(
                $"O intervalo '{fieldName}' deve estar entre {MinIntervalSeconds} e {MaxIntervalSeconds} segundos.");
        }
    }
}
