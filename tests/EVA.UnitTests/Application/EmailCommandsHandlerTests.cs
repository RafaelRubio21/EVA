using EVA.Application.Common.Interfaces;
using EVA.Application.Emails.Commands.CompleteEmailResponse;
using EVA.Application.Emails.Commands.ProcessNewEmail;
using EVA.Domain.Entities;
using EVA.Domain.Enums;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace EVA.UnitTests.Application;

public sealed class EmailCommandsHandlerTests
{
    [Fact]
    public async Task ProcessNewEmail_WhenNewMessage_ShouldAnalyzePersistAndNotifyCritical()
    {
        var repository = new InMemoryEmailRepository();
        var aiAnalyzer = new StubAiAnalyzer();
        var emailProvider = new StubEmailProvider();
        var reminderNotifier = new StubReminderNotifier();
        var handler = new ProcessNewEmailCommandHandler(
            repository,
            aiAnalyzer,
            emailProvider,
            reminderNotifier,
            NullLogger<ProcessNewEmailCommandHandler>.Instance);

        var command = new ProcessNewEmailCommand(
            "MSG-100",
            "diretor@empresa.com",
            "URGENTE: Revisar contrato",
            "Preciso de retorno até amanhã.",
            DateTimeOffset.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.EmailRecordId);
        Assert.Equal(PriorityLevel.Critical, result.PriorityLevel);
        Assert.True(result.RequiresResponse);
        Assert.Contains("MSG-100", emailProvider.MarkedAsReadIds);
        Assert.Single(reminderNotifier.CriticalAlerts);
        Assert.Equal("MSG-100", reminderNotifier.CriticalAlerts[0].MessageId);
    }

    [Fact]
    public async Task ProcessNewEmail_WhenDuplicateMessageId_ShouldThrowDomainException()
    {
        var repository = new InMemoryEmailRepository();
        var handler = new ProcessNewEmailCommandHandler(
            repository,
            new StubAiAnalyzer(),
            new StubEmailProvider(),
            new StubReminderNotifier(),
            NullLogger<ProcessNewEmailCommandHandler>.Instance);

        var command = new ProcessNewEmailCommand(
            "MSG-DUP",
            "user@empresa.com",
            "Assunto",
            "Corpo",
            DateTimeOffset.UtcNow);

        await handler.Handle(command, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteEmailResponse_WhenEmailExists_ShouldMarkCompleted()
    {
        var repository = new InMemoryEmailRepository();
        var processHandler = new ProcessNewEmailCommandHandler(
            repository,
            new StubAiAnalyzer(),
            new StubEmailProvider(),
            new StubReminderNotifier(),
            NullLogger<ProcessNewEmailCommandHandler>.Instance);

        var created = await processHandler.Handle(
            new ProcessNewEmailCommand("MSG-200", "a@b.com", "Teste", "Corpo?", DateTimeOffset.UtcNow),
            CancellationToken.None);

        var completeHandler = new CompleteEmailResponseCommandHandler(
            repository,
            NullLogger<CompleteEmailResponseCommandHandler>.Instance);

        await completeHandler.Handle(new CompleteEmailResponseCommand(created.EmailRecordId), CancellationToken.None);

        var stored = await repository.GetByIdWithAnalysisAsync(created.EmailRecordId);
        Assert.NotNull(stored);
        Assert.False(stored!.Analysis!.RequiresResponse);
    }

    private sealed class InMemoryEmailRepository : IEmailRepository
    {
        private readonly List<EmailRecord> _records = [];

        public Task<bool> ExistsByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
            => Task.FromResult(_records.Any(r => r.MessageId == messageId));

        public Task<EmailRecord?> GetByIdWithAnalysisAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_records.FirstOrDefault(r => r.Id == id));

        public Task AddAsync(EmailRecord emailRecord, CancellationToken cancellationToken = default)
        {
            _records.Add(emailRecord);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EmailRecord>> GetPendingResponsesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmailRecord>>(
                _records.Where(r => r.IsProcessed && r.Analysis?.RequiresResponse == true).ToList());

        public Task<IReadOnlyList<EmailRecord>> GetDueRemindersAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmailRecord>>(
                _records.Where(r => r.Analysis?.NextReminderAt <= referenceTime).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubAiAnalyzer : IAiAnalyzer
    {
        public Task<AiEmailAnalysisResult> AnalyzeEmailAsync(
            string sender,
            string subject,
            string body,
            DateTimeOffset receivedAt,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiEmailAnalysisResult(
                PriorityLevel.Critical,
                RequiresResponse: true,
                Summary: "Resumo de teste",
                Deadline: receivedAt.AddHours(4),
                SuggestedReminderAt: receivedAt.AddHours(1)));
        }
    }

    private sealed class StubEmailProvider : IEmailProvider
    {
        public List<string> MarkedAsReadIds { get; } = [];

        public Task<IReadOnlyList<RawEmailMessage>> FetchUnreadEmailsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RawEmailMessage>>([]);

        public Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken = default)
        {
            MarkedAsReadIds.Add(messageId);
            return Task.CompletedTask;
        }
    }

    private sealed class StubReminderNotifier : IReminderNotifier
    {
        public List<ReminderNotificationPayload> DueReminders { get; } = [];
        public List<ReminderNotificationPayload> CriticalAlerts { get; } = [];

        public Task NotifyDueReminderAsync(
            ReminderNotificationPayload payload,
            CancellationToken cancellationToken = default)
        {
            DueReminders.Add(payload);
            return Task.CompletedTask;
        }

        public Task NotifyCriticalEmailArrivedAsync(
            ReminderNotificationPayload payload,
            CancellationToken cancellationToken = default)
        {
            CriticalAlerts.Add(payload);
            return Task.CompletedTask;
        }
    }
}
