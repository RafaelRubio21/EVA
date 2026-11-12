using System.Text;
using EVA.Application.Common.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EVA.Infrastructure.Services.Email;

public sealed class GmailOAuthEmailProvider(
    IOptions<GmailOAuthOptions> options,
    ImapEmailProvider fallbackProvider,
    ILogger<GmailOAuthEmailProvider> logger) : IEmailProvider
{
    private static readonly string[] Scopes = [GmailService.Scope.GmailModify];

    private async Task<GmailService?> GetGmailServiceAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.ClientSecret))
        {
            return null;
        }

        var secrets = new ClientSecrets
        {
            ClientId = config.ClientId,
            ClientSecret = config.ClientSecret
        };

        var storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".eva",
            "gmail_auth");

        Directory.CreateDirectory(storagePath);

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            config.UserEmail,
            cancellationToken,
            new FileDataStore(storagePath, true));

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "EVA"
        });
    }

    public async Task<IReadOnlyList<RawEmailMessage>> FetchUnreadEmailsAsync(
        CancellationToken cancellationToken = default)
    {
        var service = await GetGmailServiceAsync(cancellationToken);
        if (service is null)
        {
            logger.LogDebug("[GmailOAuthEmailProvider] Credenciais OAuth do Gmail não configuradas; delegando para ImapEmailProvider.");
            return await fallbackProvider.FetchUnreadEmailsAsync(cancellationToken);
        }

        try
        {
            var config = options.Value;
            var listRequest = service.Users.Messages.List("me");
            listRequest.Q = "is:unread";
            listRequest.MaxResults = config.MaxBatchSize;

            var response = await listRequest.ExecuteAsync(cancellationToken);
            if (response.Messages is null || response.Messages.Count == 0)
            {
                return [];
            }

            var messages = new List<RawEmailMessage>();

            foreach (var item in response.Messages)
            {
                var getReq = service.Users.Messages.Get("me", item.Id);
                getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                var msg = await getReq.ExecuteAsync(cancellationToken);

                var subject = msg.Payload?.Headers?.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value ?? "(Sem assunto)";
                var sender = msg.Payload?.Headers?.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value ?? "(Desconhecido)";
                var messageIdHeader = msg.Payload?.Headers?.FirstOrDefault(h => h.Name.Equals("Message-ID", StringComparison.OrdinalIgnoreCase))?.Value;
                var finalMessageId = !string.IsNullOrWhiteSpace(messageIdHeader) ? messageIdHeader : $"GMAIL-{msg.Id}";

                DateTimeOffset receivedAt = msg.InternalDate.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds(msg.InternalDate.Value)
                    : DateTimeOffset.UtcNow;

                var body = ExtractBody(msg.Payload);

                messages.Add(new RawEmailMessage(
                    finalMessageId,
                    sender,
                    subject,
                    body,
                    receivedAt));
            }

            return messages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GmailOAuthEmailProvider] Erro ao buscar mensagens via Gmail API.");
            return await fallbackProvider.FetchUnreadEmailsAsync(cancellationToken);
        }
    }

    public async Task MarkAsReadAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var service = await GetGmailServiceAsync(cancellationToken);
        if (service is null)
        {
            await fallbackProvider.MarkAsReadAsync(messageId, cancellationToken);
            return;
        }

        try
        {
            // Busca o ID interno da mensagem no Gmail
            string? gmailId = null;
            if (messageId.StartsWith("GMAIL-", StringComparison.OrdinalIgnoreCase))
            {
                gmailId = messageId.Substring("GMAIL-".Length);
            }
            else
            {
                var searchReq = service.Users.Messages.List("me");
                searchReq.Q = $"rfc822msgid:{messageId}";
                var searchRes = await searchReq.ExecuteAsync(cancellationToken);
                gmailId = searchRes.Messages?.FirstOrDefault()?.Id;
            }

            if (!string.IsNullOrWhiteSpace(gmailId))
            {
                var mods = new ModifyMessageRequest
                {
                    RemoveLabelIds = ["UNREAD"]
                };
                await service.Users.Messages.Modify(mods, "me", gmailId).ExecuteAsync(cancellationToken);
                logger.LogInformation("[GmailOAuthEmailProvider] Mensagem '{GmailId}' marcada como lida no Gmail.", gmailId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GmailOAuthEmailProvider] Falha ao marcar mensagem '{MessageId}' como lida no Gmail.", messageId);
        }
    }

    private static string ExtractBody(MessagePart? part)
    {
        if (part is null) return string.Empty;

        if (part.MimeType == "text/plain" && !string.IsNullOrEmpty(part.Body?.Data))
        {
            return DecodeBase64(part.Body.Data);
        }

        if (part.Parts != null && part.Parts.Count > 0)
        {
            foreach (var subPart in part.Parts)
            {
                var text = ExtractBody(subPart);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        if (!string.IsNullOrEmpty(part.Body?.Data))
        {
            return DecodeBase64(part.Body.Data);
        }

        return string.Empty;
    }

    private static string DecodeBase64(string base64Url)
    {
        try
        {
            var base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}
