using EVA.Application.Accounts.Queries.GetEmailAccounts;
using MediatR;

namespace EVA.Application.Accounts.Commands.ToggleEmailAccount;

public sealed record ToggleEmailAccountCommand(Guid Id) : IRequest<EmailAccountDto>;
