using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Emails.Queries.GetEmailStats;

public sealed record GetEmailStatsQuery(string? AccountEmail = null) : IRequest<EmailStatsSummary>;
