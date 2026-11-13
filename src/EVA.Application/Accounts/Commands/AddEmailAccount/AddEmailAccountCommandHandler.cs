using EVA.Application.Accounts.Queries.GetEmailAccounts;
using EVA.Domain.Entities;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EVA.Application.Accounts.Commands.AddEmailAccount;

public sealed class AddEmailAccountCommandHandler(
    IEmailAccountRepository accountRepository,
    ILogger<AddEmailAccountCommandHandler> logger)
    : IRequestHandler<AddEmailAccountCommand, EmailAccountDto>
{
    public async Task<EmailAccountDto> Handle(
        AddEmailAccountCommand request,
        CancellationToken cancellationToken)
    {
        if (await accountRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new DomainException($"A conta '{request.Email}' já está cadastrada na EVA.");
        }

        var account = EmailAccount.Create(
            request.Email,
            request.DisplayName,
            request.ProviderType);

        await accountRepository.AddAsync(account, cancellationToken);
        await accountRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[EVA Accounts] Conta '{Email}' ({Provider}) cadastrada para monitoramento.",
            account.Email,
            account.ProviderType);

        return new EmailAccountDto(
            account.Id,
            account.Email,
            account.DisplayName,
            account.ProviderType,
            account.ProviderType.ToString(),
            account.IsActive,
            account.CreatedAt,
            account.LastSyncedAt);
    }
}
