using EVA.Application.Accounts.Queries.GetEmailAccounts;
using EVA.Domain.Entities;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Accounts.Commands.ToggleEmailAccount;

public sealed class ToggleEmailAccountCommandHandler(IEmailAccountRepository accountRepository)
    : IRequestHandler<ToggleEmailAccountCommand, EmailAccountDto>
{
    public async Task<EmailAccountDto> Handle(
        ToggleEmailAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmailAccount), request.Id);

        account.ToggleActive();
        await accountRepository.SaveChangesAsync(cancellationToken);

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
