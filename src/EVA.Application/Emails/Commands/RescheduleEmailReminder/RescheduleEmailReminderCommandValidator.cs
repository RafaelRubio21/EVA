using FluentValidation;

namespace EVA.Application.Emails.Commands.RescheduleEmailReminder;

public sealed class RescheduleEmailReminderCommandValidator
    : AbstractValidator<RescheduleEmailReminderCommand>
{
    public RescheduleEmailReminderCommandValidator()
    {
        RuleFor(x => x.EmailId)
            .NotEmpty()
            .WithMessage("O identificador do e-mail (EmailId) é obrigatório.");

        RuleFor(x => x.NextReminderAt)
            .Must(date => date > DateTimeOffset.UtcNow)
            .WithMessage("A nova data do lembrete (NextReminderAt) deve estar no futuro.");
    }
}
