using MediatR;

namespace EVA.Application.Accounts.Commands.DeleteEmailAccount;

public sealed record DeleteEmailAccountCommand(Guid Id) : IRequest;
