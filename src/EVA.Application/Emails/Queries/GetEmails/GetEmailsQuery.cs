using EVA.Application.Common.Models;
using EVA.Domain.Enums;
using MediatR;

namespace EVA.Application.Emails.Queries.GetEmails;

public sealed record GetEmailsQuery(
    string? AccountEmail = null,
    PriorityLevel? PriorityLevel = null,
    bool? RequiresResponse = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<EmailSummaryDto>>;
