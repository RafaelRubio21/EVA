using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Accounts.Queries.GetEmailAccounts;

public sealed class GetEmailAccountsQueryHandler(IEmailAccountRepository accountRepository)
    : IRequestHandler<GetEmailAccountsQuery, IReadOnlyList<EmailAccountDto>>
{
    public async Task<IReadOnlyList<EmailAccountDto>> Handle(
        GetEmailAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await accountRepository.GetAllAsync(cancellationToken);

        return accounts
            .Select(a => new EmailAccountDto(
                a.Id,
                a.Email,
                a.DisplayName,
                a.ProviderType,
                a.ProviderType.ToString(),
                a.IsActive,
                a.CreatedAt,
                a.LastSyncedAt))
            .ToList();
    }
}
