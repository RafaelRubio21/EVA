using System;
using System.Collections.Generic;

namespace EVA.Desktop.Models;

public enum PriorityLevel
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum EmailProviderType
{
    GmailOAuth = 1,
    Imap = 2
}

public sealed record AccountEmailStats(string AccountEmail, int TotalEmails, int PendingResponses);

public sealed record EmailStatsSummary(
    int TotalEmails,
    int PendingResponses,
    int OverdueDeadlines,
    int CriticalPending,
    int HighPending,
    int NormalPending,
    int LowPending,
    IReadOnlyList<AccountEmailStats> ByAccount);

public sealed record EmailSummaryDto(
    Guid Id,
    string MessageId,
    string AccountEmail,
    string Sender,
    string Subject,
    DateTimeOffset ReceivedAt,
    bool IsProcessed,
    PriorityLevel? PriorityLevel,
    string? PriorityLabel,
    bool? RequiresResponse,
    string? Summary,
    string? SuggestedReplyDraft,
    DateTimeOffset? Deadline,
    DateTimeOffset? NextReminderAt)
{
    public string PriorityColor => PriorityLevel switch
    {
        Models.PriorityLevel.Critical => "#EF4444",
        Models.PriorityLevel.High => "#F97316",
        Models.PriorityLevel.Normal => "#3B82F6",
        _ => "#71717A"
    };

    public string ReceivedAtFormatted => ReceivedAt.ToLocalTime().ToString("dd/MM HH:mm");
    public string DeadlineFormatted => Deadline.HasValue ? $"Prazo: {Deadline.Value.ToLocalTime():dd/MM HH:mm}" : "Sem prazo";
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record EmailDetailsDto(
    Guid Id,
    string MessageId,
    string AccountEmail,
    string Sender,
    string Subject,
    string Body,
    DateTimeOffset ReceivedAt,
    bool IsProcessed,
    PriorityLevel? PriorityLevel,
    string? PriorityLabel,
    bool? RequiresResponse,
    string? Summary,
    string? SuggestedReplyDraft,
    DateTimeOffset? Deadline,
    DateTimeOffset? NextReminderAt);

public sealed record EmailAccountDto(
    Guid Id,
    string Email,
    string DisplayName,
    EmailProviderType ProviderType,
    string ProviderLabel,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSyncedAt)
{
    public string StatusLabel => IsActive ? "Ativa" : "Pausada";
    public string StatusColor => IsActive ? "#10B981" : "#71717A";
    public string LastSyncFormatted => LastSyncedAt.HasValue
        ? $"Última sinc.: {LastSyncedAt.Value.ToLocalTime():dd/MM HH:mm}"
        : "Aguardando 1ª sincronização";
}

public sealed record EvaSettingsDto(
    int EmailPollingIntervalSeconds,
    int ReminderCheckIntervalSeconds,
    DateTimeOffset UpdatedAt);

public sealed record ReminderNotificationPayload(
    Guid EmailId,
    string MessageId,
    string Sender,
    string Subject,
    PriorityLevel PriorityLevel,
    string Summary,
    DateTimeOffset? Deadline,
    DateTimeOffset? NextReminderAt);

public sealed class DesktopAppConfig
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5199";
    public string ApiKey { get; set; } = string.Empty;
}
