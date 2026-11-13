namespace EVA.Application.Common.Interfaces;

public sealed record RawEmailMessage(
    string MessageId,
    string AccountEmail,
    string Sender,
    string Subject,
    string Body,
    DateTimeOffset ReceivedAt);

public interface IEmailProvider
{
    Task<IReadOnlyList<RawEmailMessage>> FetchUnreadEmailsAsync(
        string? accountEmail = null,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        string messageId,
        string? accountEmail = null,
        CancellationToken cancellationToken = default);
}
