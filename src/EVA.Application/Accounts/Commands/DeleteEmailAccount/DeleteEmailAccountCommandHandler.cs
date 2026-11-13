using EVA.Domain.Entities;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Accounts.Commands.DeleteEmailAccount;

public sealed class DeleteEmailAccountCommandHandler(IEmailAccountRepository accountRepository)
    : IRequestHandler<DeleteEmailAccountCommand>
{
    public async Task Handle(
        DeleteEmailAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmailAccount), request.Id);

        await accountRepository.DeleteAsync(account, cancellationToken);
        await accountRepository.SaveChangesAsync(cancellationToken);
    }
}
