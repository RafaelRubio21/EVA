using EVA.Application.Accounts.Commands.AddEmailAccount;
using EVA.Application.Accounts.Commands.ToggleEmailAccount;
using EVA.Domain.Entities;
using EVA.Domain.Enums;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace EVA.UnitTests.Application;

public sealed class EmailAccountTests
{
    [Fact]
    public async Task AddEmailAccount_WhenNewEmail_ShouldCreateActiveNormalizedAccount()
    {
        var repository = new InMemoryEmailAccountRepository();
        var handler = new AddEmailAccountCommandHandler(
            repository,
            NullLogger<AddEmailAccountCommandHandler>.Instance);

        var result = await handler.Handle(
            new AddEmailAccountCommand("Rafael.Rubio@Estapar.com.br", "Estapar Corporativo", EmailProviderType.GmailOAuth),
            CancellationToken.None);

        Assert.Equal("rafael.rubio@estapar.com.br", result.Email);
        Assert.True(result.IsActive);
        Assert.Equal(EmailProviderType.GmailOAuth, result.ProviderType);
    }

    [Fact]
    public async Task AddEmailAccount_WhenDuplicate_ShouldThrowDomainException()
    {
        var repository = new InMemoryEmailAccountRepository();
        var handler = new AddEmailAccountCommandHandler(
            repository,
            NullLogger<AddEmailAccountCommandHandler>.Instance);

        await handler.Handle(
            new AddEmailAccountCommand("rafael3j@gmail.com", "Pessoal"),
            CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AddEmailAccountCommand("RAFAEL3J@GMAIL.COM", "Duplicada"), CancellationToken.None));
    }

    [Fact]
    public async Task ToggleEmailAccount_ShouldFlipIsActiveFlag()
    {
        var repository = new InMemoryEmailAccountRepository();
        var addHandler = new AddEmailAccountCommandHandler(
            repository,
            NullLogger<AddEmailAccountCommandHandler>.Instance);

        var created = await addHandler.Handle(
            new AddEmailAccountCommand("rafael3j@gmail.com", "Pessoal"),
            CancellationToken.None);

        var toggleHandler = new ToggleEmailAccountCommandHandler(repository);
        var toggled = await toggleHandler.Handle(new ToggleEmailAccountCommand(created.Id), CancellationToken.None);

        Assert.False(toggled.IsActive);
    }

    private sealed class InMemoryEmailAccountRepository : IEmailAccountRepository
    {
        private readonly List<EmailAccount> _accounts = [];

        public Task<IReadOnlyList<EmailAccount>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmailAccount>>(_accounts.ToList());

        public Task<IReadOnlyList<EmailAccount>> GetActiveAccountsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmailAccount>>(_accounts.Where(a => a.IsActive).ToList());

        public Task<EmailAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_accounts.FirstOrDefault(a => a.Id == id));

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_accounts.Any(a => a.Email == normalized));
        }

        public Task AddAsync(EmailAccount account, CancellationToken cancellationToken = default)
        {
            _accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EmailAccount account, CancellationToken cancellationToken = default)
        {
            _accounts.Remove(account);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
