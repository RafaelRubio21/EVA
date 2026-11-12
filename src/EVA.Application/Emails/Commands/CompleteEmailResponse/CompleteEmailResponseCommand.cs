using MediatR;

namespace EVA.Application.Emails.Commands.CompleteEmailResponse;

public sealed record CompleteEmailResponseCommand(Guid EmailId) : IRequest;
