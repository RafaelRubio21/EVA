using EVA.Application.Accounts.Queries.GetEmailAccounts;
using EVA.Domain.Enums;
using MediatR;

namespace EVA.Application.Accounts.Commands.AddEmailAccount;

public sealed record AddEmailAccountCommand(
    string Email,
    string DisplayName,
    EmailProviderType ProviderType = EmailProviderType.GmailOAuth) : IRequest<EmailAccountDto>;
