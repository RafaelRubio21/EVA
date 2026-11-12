using EVA.Domain.Entities;
using EVA.Domain.Enums;
using EVA.Domain.Exceptions;

namespace EVA.UnitTests.Domain;

public sealed class EmailRecordTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInitializeUnprocessedRecord()
    {
        var receivedAt = DateTimeOffset.UtcNow;

        var record = EmailRecord.Create(
            "MSG-001",
            "rafael@estapar.com.br",
            "cliente@empresa.com",
            "Proposta comercial",
            "Segue anexo para avaliação.",
            receivedAt);

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal("MSG-001", record.MessageId);
        Assert.Equal("rafael@estapar.com.br", record.AccountEmail);
        Assert.Equal("cliente@empresa.com", record.Sender);
        Assert.Equal("Proposta comercial", record.Subject);
        Assert.False(record.IsProcessed);
        Assert.Null(record.Analysis);
    }

    [Theory]
    [InlineData("", "conta@test.com", "sender@test.com", "Assunto")]
    [InlineData("MSG-1", "", "sender@test.com", "Assunto")]
    [InlineData("MSG-1", "conta@test.com", "", "Assunto")]
    [InlineData("MSG-1", "conta@test.com", "sender@test.com", "")]
    public void Create_WithMissingRequiredFields_ShouldThrowDomainException(
        string messageId,
        string accountEmail,
        string sender,
        string subject)
    {
        Assert.Throws<DomainException>(() =>
            EmailRecord.Create(messageId, accountEmail, sender, subject, "Corpo", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AttachAnalysis_ShouldMarkAsProcessedAndLinkAnalysis()
    {
        var record = EmailRecord.Create("MSG-002", "conta@test.com", "a@b.com", "Assunto", "Corpo", DateTimeOffset.UtcNow);
        var analysis = EmailAnalysis.Create(
            record.Id,
            PriorityLevel.High,
            requiresResponse: true,
            summary: "Resumo executivo");

        record.AttachAnalysis(analysis);

        Assert.True(record.IsProcessed);
        Assert.NotNull(record.Analysis);
        Assert.Equal(PriorityLevel.High, record.Analysis!.PriorityLevel);
        Assert.NotNull(record.Analysis.NextReminderAt);
    }

    [Fact]
    public void CompleteResponse_ShouldClearRequiresResponseAndReminder()
    {
        var record = EmailRecord.Create("MSG-003", "conta@test.com", "a@b.com", "Assunto", "Corpo", DateTimeOffset.UtcNow);
        var analysis = EmailAnalysis.Create(
            record.Id,
            PriorityLevel.Critical,
            requiresResponse: true,
            summary: "Resumo urgente");
        record.AttachAnalysis(analysis);

        record.CompleteResponse();

        Assert.False(record.Analysis!.RequiresResponse);
        Assert.Null(record.Analysis.NextReminderAt);
    }

    [Fact]
    public void RescheduleReminder_AfterDeadline_ShouldThrowDomainException()
    {
        var now = DateTimeOffset.UtcNow;
        var deadline = now.AddHours(4);

        var record = EmailRecord.Create("MSG-004", "conta@test.com", "a@b.com", "Assunto", "Corpo", now);
        var analysis = EmailAnalysis.Create(
            record.Id,
            PriorityLevel.High,
            requiresResponse: true,
            summary: "Com prazo",
            deadline: deadline,
            nextReminderAt: now.AddHours(1));
        record.AttachAnalysis(analysis);

        Assert.Throws<DomainException>(() =>
            record.RescheduleReminder(deadline.AddHours(1)));
    }

    [Fact]
    public void RegisterReminderSent_ShouldAdvanceNextReminderByPrioritySnoozeInterval()
    {
        var now = DateTimeOffset.UtcNow;
        var record = EmailRecord.Create("MSG-005", "conta@test.com", "a@b.com", "Assunto", "Corpo", now);
        var analysis = EmailAnalysis.Create(
            record.Id,
            PriorityLevel.Critical,
            requiresResponse: true,
            summary: "Crítico",
            nextReminderAt: now.AddMinutes(5));
        record.AttachAnalysis(analysis);

        record.RegisterReminderSent(now);

        Assert.Equal(now.AddMinutes(15), record.Analysis!.NextReminderAt);
    }
}
