using EVA.Application.Settings.Commands.UpdateEvaSettings;
using EVA.Domain.Entities;
using EVA.Domain.Exceptions;
using EVA.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace EVA.UnitTests.Application;

public sealed class EvaSettingsTests
{
    [Fact]
    public void CreateDefault_ShouldUseDefaultIntervals()
    {
        var settings = EvaSettings.CreateDefault();

        Assert.Equal(EvaSettings.DefaultEmailPollingSeconds, settings.EmailPollingIntervalSeconds);
        Assert.Equal(EvaSettings.DefaultReminderCheckSeconds, settings.ReminderCheckIntervalSeconds);
    }

    [Theory]
    [InlineData(5, 60)]
    [InlineData(120, 10)]
    [InlineData(90000, 60)]
    public void UpdateIntervals_OutOfAllowedRange_ShouldThrowDomainException(
        int emailSeconds,
        int reminderSeconds)
    {
        var settings = EvaSettings.CreateDefault();

        Assert.Throws<DomainException>(() =>
            settings.UpdateIntervals(emailSeconds, reminderSeconds));
    }

    [Fact]
    public async Task UpdateEvaSettingsHandler_WhenNoSettingsExist_ShouldCreateAndSave()
    {
        var repository = new InMemorySettingsRepository();
        var handler = new UpdateEvaSettingsCommandHandler(
            repository,
            NullLogger<UpdateEvaSettingsCommandHandler>.Instance);

        var result = await handler.Handle(
            new UpdateEvaSettingsCommand(300, 45),
            CancellationToken.None);

        Assert.Equal(300, result.EmailPollingIntervalSeconds);
        Assert.Equal(45, result.ReminderCheckIntervalSeconds);
        Assert.NotNull(repository.Current);
    }

    private sealed class InMemorySettingsRepository : IEvaSettingsRepository
    {
        public EvaSettings? Current { get; private set; }

        public Task<EvaSettings?> GetCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Current);

        public Task AddAsync(EvaSettings settings, CancellationToken cancellationToken = default)
        {
            Current = settings;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
