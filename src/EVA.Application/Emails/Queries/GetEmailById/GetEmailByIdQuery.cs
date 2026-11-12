using MediatR;

namespace EVA.Application.Emails.Queries.GetEmailById;

public sealed record GetEmailByIdQuery(Guid Id) : IRequest<EmailDetailsDto>;
