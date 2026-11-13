using EVA.Application.Common.Interfaces;
using EVA.Application.Emails.Commands.CompleteEmailResponse;
using EVA.Application.Emails.Commands.ProcessNewEmail;
using EVA.Application.Emails.Queries.GetEmails;
using EVA.Application.Emails.Queries.GetEmailStats;
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
            "rafael@estapar.com.br",
            "diretor@empresa.com",
            "URGENTE: Revisar contrato",
            "Preciso de retorno até amanhã.",
            DateTimeOffset.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.EmailRecordId);
        Assert.Equal(PriorityLevel.Critical, result.PriorityLevel);
        Assert.True(result.RequiresResponse);
        Assert.Equal("Rascunho sugerido de teste", result.SuggestedReplyDraft);
        Assert.Contains("MSG-100", emailProvider.MarkedAsReadIds);
        Assert.Single(reminderNotifier.CriticalAlerts);
        Assert.Equal("MSG-100", reminderNotifier.CriticalAlerts[0].MessageId);
    }

    [Fact]
    public async Task GetEmails_ShouldFilterByAccountEmailAndPaginate()
    {
        var repository = new InMemoryEmailRepository();
        var processHandler = new ProcessNewEmailCommandHandler(
            repository,
            new StubAiAnalyzer(),
            new StubEmailProvider(),
            new StubReminderNotifier(),
            NullLogger<ProcessNewEmailCommandHandler>.Instance);

        await processHandler.Handle(
            new ProcessNewEmailCommand("MSG-A1", "rafael@estapar.com.br", "a@x.com", "A1", "Corpo", DateTimeOffset.UtcNow),
            CancellationToken.None);
        await processHandler.Handle(
            new ProcessNewEmailCommand("MSG-B1", "rafael3j@gmail.com", "b@x.com", "B1", "Corpo", DateTimeOffset.UtcNow),
            CancellationToken.None);

        var queryHandler = new GetEmailsQueryHandler(repository);
        var paged = await queryHandler.Handle(
            new GetEmailsQuery(AccountEmail: "rafael@estapar.com.br", Page: 1, PageSize: 10),
            CancellationToken.None);

        Assert.Equal(1, paged.TotalCount);
        Assert.Single(paged.Items);
        Assert.Equal("MSG-A1", paged.Items[0].MessageId);
    }

    [Fact]
    public async Task GetEmailStats_ShouldAggregateTotalsAndAccountBreakdown()
    {
        var repository = new InMemoryEmailRepository();
        var processHandler = new ProcessNewEmailCommandHandler(
            repository,
            new StubAiAnalyzer(),
            new StubEmailProvider(),
            new StubReminderNotifier(),
            NullLogger<ProcessNewEmailCommandHandler>.Instance);

        await processHandler.Handle(
            new ProcessNewEmailCommand("MSG-S1", "rafael@estapar.com.br", "a@x.com", "S1", "Corpo", DateTimeOffset.UtcNow.AddHours(-10)),
            CancellationToken.None);
        await processHandler.Handle(
            new ProcessNewEmailCommand("MSG-S2", "rafael3j@gmail.com", "b@x.com", "S2", "Corpo", DateTimeOffset.UtcNow),
            CancellationToken.None);

        var statsHandler = new GetEmailStatsQueryHandler(repository);
        var stats = await statsHandler.Handle(new GetEmailStatsQuery(), CancellationToken.None);

        Assert.Equal(2, stats.TotalEmails);
        Assert.Equal(2, stats.PendingResponses);
        Assert.Equal(1, stats.OverdueDeadlines);
        Assert.Equal(2, stats.CriticalPending);
        Assert.Equal(2, stats.ByAccount.Count);
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
            "rafael@estapar.com.br",
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
            new ProcessNewEmailCommand("MSG-200", "rafael@estapar.com.br", "a@b.com", "Teste", "Corpo?", DateTimeOffset.UtcNow),
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

        public Task<IReadOnlyList<EmailRecord>> GetPendingResponsesAsync(
            string? accountEmail = null,
            PriorityLevel? priorityLevel = null,
            CancellationToken cancellationToken = default)
        {
            var query = _records.Where(r => r.IsProcessed && r.Analysis?.RequiresResponse == true);
            if (!string.IsNullOrWhiteSpace(accountEmail))
                query = query.Where(r => r.AccountEmail == accountEmail.Trim().ToLowerInvariant());
            if (priorityLevel.HasValue)
                query = query.Where(r => r.Analysis?.PriorityLevel == priorityLevel.Value);

            return Task.FromResult<IReadOnlyList<EmailRecord>>(query.ToList());
        }

        public Task<(IReadOnlyList<EmailRecord> Items, int TotalCount)> GetPagedAsync(
            string? accountEmail,
            PriorityLevel? priorityLevel,
            bool? requiresResponse,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _records.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(accountEmail))
                query = query.Where(r => r.AccountEmail == accountEmail.Trim().ToLowerInvariant());
            if (priorityLevel.HasValue)
                query = query.Where(r => r.Analysis?.PriorityLevel == priorityLevel.Value);
            if (requiresResponse.HasValue)
                query = query.Where(r => r.Analysis?.RequiresResponse == requiresResponse.Value);

            var filtered = query.OrderByDescending(r => r.ReceivedAt).ToList();
            var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult<(IReadOnlyList<EmailRecord>, int)>((items, filtered.Count));
        }

        public Task<EmailStatsSummary> GetStatsAsync(
            string? accountEmail,
            DateTimeOffset referenceTime,
            CancellationToken cancellationToken = default)
        {
            var query = _records.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(accountEmail))
                query = query.Where(r => r.AccountEmail == accountEmail.Trim().ToLowerInvariant());

            var list = query.ToList();
            var pending = list.Where(r => r.IsProcessed && r.Analysis?.RequiresResponse == true).ToList();
            var byAccount = list
                .GroupBy(r => r.AccountEmail)
                .Select(g => new AccountEmailStats(
                    g.Key,
                    g.Count(),
                    g.Count(r => r.IsProcessed && r.Analysis?.RequiresResponse == true)))
                .OrderBy(a => a.AccountEmail)
                .ToList();

            return Task.FromResult(new EmailStatsSummary(
                list.Count,
                pending.Count,
                pending.Count(r => r.Analysis!.Deadline < referenceTime),
                pending.Count(r => r.Analysis!.PriorityLevel == PriorityLevel.Critical),
                pending.Count(r => r.Analysis!.PriorityLevel == PriorityLevel.High),
                pending.Count(r => r.Analysis!.PriorityLevel == PriorityLevel.Normal),
                pending.Count(r => r.Analysis!.PriorityLevel == PriorityLevel.Low),
                byAccount));
        }

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
                SuggestedReminderAt: receivedAt.AddHours(1),
                SuggestedReplyDraft: "Rascunho sugerido de teste"));
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
