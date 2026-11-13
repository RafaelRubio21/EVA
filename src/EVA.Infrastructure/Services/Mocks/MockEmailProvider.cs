using EVA.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVA.Infrastructure.Services.Mocks;

public sealed class MockEmailProvider(ILogger<MockEmailProvider> logger) : IEmailProvider
{
    public Task<IReadOnlyList<RawEmailMessage>> FetchUnreadEmailsAsync(
        string? accountEmail = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[MockEmailProvider] Retornando lote simulado de e-mails não lidos.");

        var targetAccount = string.IsNullOrWhiteSpace(accountEmail)
            ? "usuario@estapar.com.br"
            : accountEmail.Trim().ToLowerInvariant();

        IReadOnlyList<RawEmailMessage> emails =
        [
            new RawEmailMessage(
                MessageId: "MSG-2026-001",
                AccountEmail: targetAccount,
                Sender: "diretoria@empresa.com.br",
                Subject: "URGENTE: Aprovação do orçamento de infraestrutura Q4",
                Body: "Olá, precisamos confirmar a aprovação do orçamento até o final do dia. Aguardo retorno.",
                ReceivedAt: DateTimeOffset.UtcNow.AddMinutes(-15)),
            new RawEmailMessage(
                MessageId: "MSG-2026-002",
                AccountEmail: targetAccount,
                Sender: "newsletter@techweekly.io",
                Subject: "Resumo semanal de novidades .NET 10",
                Body: "Confira os principais lançamentos da semana no ecossistema .NET.",
                ReceivedAt: DateTimeOffset.UtcNow.AddHours(-2))
        ];

        return Task.FromResult(emails);
    }

    public Task MarkAsReadAsync(
        string messageId,
        string? accountEmail = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[MockEmailProvider] E-mail '{MessageId}' marcado como lido no servidor de origem.",
            messageId);

        return Task.CompletedTask;
    }
}
