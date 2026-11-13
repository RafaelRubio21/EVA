using EVA.Domain.Common;
using EVA.Domain.Enums;
using EVA.Domain.Exceptions;

namespace EVA.Domain.Entities;

public sealed class EmailAccount : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public EmailProviderType ProviderType { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastSyncedAt { get; private set; }

    private EmailAccount() { }

    private EmailAccount(
        string email,
        string displayName,
        EmailProviderType providerType)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O endereço de e-mail da conta é obrigatório.");

        Email = email.Trim().ToLowerInvariant();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Email : displayName.Trim();
        ProviderType = providerType;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static EmailAccount Create(
        string email,
        string displayName,
        EmailProviderType providerType = EmailProviderType.GmailOAuth)
    {
        return new EmailAccount(email, displayName, providerType);
    }

    public void ToggleActive() => IsActive = !IsActive;

    public void MarkSynced(DateTimeOffset syncedAt) => LastSyncedAt = syncedAt;
}
