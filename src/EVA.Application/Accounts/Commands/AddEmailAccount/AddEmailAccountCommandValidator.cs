using FluentValidation;

namespace EVA.Application.Accounts.Commands.AddEmailAccount;

public sealed class AddEmailAccountCommandValidator : AbstractValidator<AddEmailAccountCommand>
{
    public AddEmailAccountCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail da conta é obrigatório.")
            .EmailAddress().WithMessage("Informe um endereço de e-mail válido.")
            .MaximumLength(320);

        RuleFor(x => x.DisplayName)
            .MaximumLength(150);
    }
}
