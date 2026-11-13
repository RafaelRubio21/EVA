using EVA.Domain.Enums;

namespace EVA.Application.Accounts.Queries.GetEmailAccounts;

public sealed record EmailAccountDto(
    Guid Id,
    string Email,
    string DisplayName,
    EmailProviderType ProviderType,
    string ProviderLabel,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSyncedAt);
