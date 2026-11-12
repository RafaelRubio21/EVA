using EVA.Application.Common.Interfaces;
using EVA.Infrastructure.Services.Mocks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EVA.Infrastructure.Services.Email;

public sealed class ImapEmailProvider(
    IOptions<ImapEmailOptions> options,
    MockEmailProvider fallbackProvider,
    ILogger<ImapEmailProvider> logger) : IEmailProvider
{
    public async Task<IReadOnlyList<RawEmailMessage>> FetchUnreadEmailsAsync(
        CancellationToken cancellationToken = default)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(config.Host) || string.IsNullOrWhiteSpace(config.Username))
        {
            logger.LogDebug("[ImapEmailProvider] Credenciais IMAP não configuradas; utilizando MockEmailProvider.");
            return await fallbackProvider.FetchUnreadEmailsAsync(cancellationToken);
        }

        using var client = new ImapClient();
        await client.ConnectAsync(config.Host, config.Port, config.UseSsl, cancellationToken);
        await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
        var results = new List<RawEmailMessage>();

        foreach (var uid in uids.Take(config.MaxBatchSize))
        {
            var message = await inbox.GetMessageAsync(uid, cancellationToken);
            var messageId = !string.IsNullOrWhiteSpace(message.MessageId)
                ? message.MessageId
                : $"IMAP-UID-{uid.Id}";

            var sender = message.From.Mailboxes.FirstOrDefault()?.Address
                ?? message.From.ToString();

            var body = !string.IsNullOrWhiteSpace(message.TextBody)
                ? message.TextBody
                : message.HtmlBody ?? "(Sem conteúdo textual)";

            results.Add(new RawEmailMessage(
                messageId,
                config.Username,
                sender,
                message.Subject ?? "(Sem Assunto)",
                body,
                message.Date));
        }

        await client.DisconnectAsync(quit: true, cancellationToken);
        return results;
    }

    public async Task MarkAsReadAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(config.Host) || string.IsNullOrWhiteSpace(config.Username))
        {
            await fallbackProvider.MarkAsReadAsync(messageId, cancellationToken);
            return;
        }

        using var client = new ImapClient();
        await client.ConnectAsync(config.Host, config.Port, config.UseSsl, cancellationToken);
        await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        var uids = await inbox.SearchAsync(
            SearchQuery.HeaderContains("Message-Id", messageId),
            cancellationToken);

        if (uids.Count > 0)
        {
            await inbox.AddFlagsAsync(uids, MessageFlags.Seen, silent: true, cancellationToken);
        }

        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
