using EVA.Domain.Common;
using EVA.Domain.Exceptions;

namespace EVA.Domain.Entities;

public sealed class EmailRecord : Entity
{
    public string MessageId { get; private set; } = string.Empty;
    public string Sender { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
    public bool IsProcessed { get; private set; }

    public EmailAnalysis? Analysis { get; private set; }

    // Construtor protegido/privado exigido pelo EF Core
    private EmailRecord() { }

    private EmailRecord(
        string messageId,
        string sender,
        string subject,
        string body,
        DateTimeOffset receivedAt)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new DomainException("O identificador da mensagem (MessageId) é obrigatório.");

        if (string.IsNullOrWhiteSpace(sender))
            throw new DomainException("O remetente (Sender) é obrigatório.");

        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("O assunto (Subject) é obrigatório.");

        MessageId = messageId.Trim();
        Sender = sender.Trim();
        Subject = subject.Trim();
        Body = body ?? string.Empty;
        ReceivedAt = receivedAt;
        IsProcessed = false;
    }

    public static EmailRecord Create(
        string messageId,
        string sender,
        string subject,
        string body,
        DateTimeOffset receivedAt)
    {
        return new EmailRecord(messageId, sender, subject, body, receivedAt);
    }

    public void AttachAnalysis(EmailAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        if (IsProcessed)
            throw new DomainException($"O e-mail '{MessageId}' já foi processado anteriormente.");

        if (analysis.EmailRecordId != Id)
            throw new DomainException("A análise informada pertence a outro registro de e-mail.");

        Analysis = analysis;
        IsProcessed = true;
    }

    public void CompleteResponse()
    {
        if (!IsProcessed || Analysis is null)
            throw new DomainException("Não é possível concluir a resposta de um e-mail ainda não processado.");

        Analysis.MarkResponseCompleted();
    }

    public void RescheduleReminder(DateTimeOffset newReminderAt)
    {
        if (!IsProcessed || Analysis is null)
            throw new DomainException("Não é possível reagendar lembrete de um e-mail ainda não processado.");

        Analysis.RescheduleReminder(newReminderAt);
    }
}
