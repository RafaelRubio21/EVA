using EVA.Domain.Entities;
using FluentValidation;

namespace EVA.Application.Settings.Commands.UpdateEvaSettings;

public sealed class UpdateEvaSettingsCommandValidator : AbstractValidator<UpdateEvaSettingsCommand>
{
    public UpdateEvaSettingsCommandValidator()
    {
        RuleFor(x => x.EmailPollingIntervalSeconds)
            .InclusiveBetween(EvaSettings.MinIntervalSeconds, EvaSettings.MaxIntervalSeconds)
            .WithMessage($"O intervalo de leitura de e-mails deve estar entre {EvaSettings.MinIntervalSeconds} e {EvaSettings.MaxIntervalSeconds} segundos.");

        RuleFor(x => x.ReminderCheckIntervalSeconds)
            .InclusiveBetween(EvaSettings.MinIntervalSeconds, EvaSettings.MaxIntervalSeconds)
            .WithMessage($"O intervalo de verificação de lembretes deve estar entre {EvaSettings.MinIntervalSeconds} e {EvaSettings.MaxIntervalSeconds} segundos.");
    }
}
