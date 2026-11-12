using FluentValidation;

namespace EVA.Application.Emails.Commands.CompleteEmailResponse;

public sealed class CompleteEmailResponseCommandValidator
    : AbstractValidator<CompleteEmailResponseCommand>
{
    public CompleteEmailResponseCommandValidator()
    {
        RuleFor(x => x.EmailId)
            .NotEmpty()
            .WithMessage("O identificador do e-mail (EmailId) é obrigatório.");
    }
}
