using FluentValidation;

namespace EVA.Application.Emails.Queries.GetEmails;

public sealed class GetEmailsQueryValidator : AbstractValidator<GetEmailsQuery>
{
    public GetEmailsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("O número da página (Page) deve ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("O tamanho da página (PageSize) deve estar entre 1 e 100.");
    }
}
