using FluentValidation;

namespace EVA.Application.Emails.Commands.ProcessNewEmail;

public sealed class ProcessNewEmailCommandValidator : AbstractValidator<ProcessNewEmailCommand>
{
    public ProcessNewEmailCommandValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty().WithMessage("O MessageId do e-mail é obrigatório.")
            .MaximumLength(256);

        RuleFor(x => x.Sender)
            .NotEmpty().WithMessage("O remetente (Sender) é obrigatório.")
            .EmailAddress().WithMessage("O remetente deve ser um endereço de e-mail válido.")
            .MaximumLength(320);

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("O assunto (Subject) é obrigatório.")
            .MaximumLength(500);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("O corpo do e-mail (Body) não pode estar vazio.");
    }
}
