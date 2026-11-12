using EVA.Domain.Common;
using EVA.Domain.Enums;
using EVA.Domain.Exceptions;

namespace EVA.Domain.Entities;

public sealed class EmailAnalysis : Entity
{
    public Guid EmailRecordId { get; private set; }
    public PriorityLevel PriorityLevel { get; private set; }
    public bool RequiresResponse { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset? Deadline { get; private set; }
    public DateTimeOffset? NextReminderAt { get; private set; }

    // Construtor privado exigido pelo EF Core
    private EmailAnalysis() { }

    private EmailAnalysis(
        Guid emailRecordId,
        PriorityLevel priorityLevel,
        bool requiresResponse,
        string summary,
        DateTimeOffset? deadline,
        DateTimeOffset? nextReminderAt)
    {
        if (emailRecordId == Guid.Empty)
            throw new DomainException("A análise deve estar associada a um EmailRecord válido.");

        if (string.IsNullOrWhiteSpace(summary))
            throw new DomainException("O resumo (Summary) gerado pela análise é obrigatório.");

        if (deadline.HasValue && nextReminderAt.HasValue && nextReminderAt > deadline)
            throw new DomainException("O próximo lembrete (NextReminderAt) não pode ser posterior ao prazo final (Deadline).");

        EmailRecordId = emailRecordId;
        PriorityLevel = priorityLevel;
        RequiresResponse = requiresResponse;
        Summary = summary.Trim();
        Deadline = deadline;
        NextReminderAt = requiresResponse
            ? nextReminderAt ?? CalculateDefaultReminder(priorityLevel, deadline)
            : null;
    }

    public static EmailAnalysis Create(
        Guid emailRecordId,
        PriorityLevel priorityLevel,
        bool requiresResponse,
        string summary,
        DateTimeOffset? deadline = null,
        DateTimeOffset? nextReminderAt = null)
    {
        return new EmailAnalysis(
            emailRecordId,
            priorityLevel,
            requiresResponse,
            summary,
            deadline,
            nextReminderAt);
    }

    public void RescheduleReminder(DateTimeOffset newReminderAt)
    {
        if (!RequiresResponse)
            throw new DomainException("Não é possível agendar lembretes para um e-mail que não requer resposta.");

        if (Deadline.HasValue && newReminderAt > Deadline.Value)
            throw new DomainException("O lembrete não pode ultrapassar o prazo final (Deadline).");

        NextReminderAt = newReminderAt;
    }

    public void MarkResponseCompleted()
    {
        RequiresResponse = false;
        NextReminderAt = null;
    }

    private static DateTimeOffset CalculateDefaultReminder(
        PriorityLevel priority,
        DateTimeOffset? deadline)
    {
        var now = DateTimeOffset.UtcNow;
        var suggested = priority switch
        {
            PriorityLevel.Critical => now.AddMinutes(30),
            PriorityLevel.High => now.AddHours(2),
            PriorityLevel.Normal => now.AddHours(8),
            _ => now.AddHours(24)
        };

        return deadline.HasValue && suggested > deadline.Value
            ? deadline.Value.AddMinutes(-15)
            : suggested;
    }
}
