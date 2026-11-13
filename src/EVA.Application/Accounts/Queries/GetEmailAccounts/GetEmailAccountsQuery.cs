using MediatR;

namespace EVA.Application.Accounts.Queries.GetEmailAccounts;

public sealed record GetEmailAccountsQuery : IRequest<IReadOnlyList<EmailAccountDto>>;
