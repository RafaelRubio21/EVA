using EVA.Domain.Repositories;
using MediatR;

namespace EVA.Application.Emails.Queries.GetEmailStats;

public sealed class GetEmailStatsQueryHandler(IEmailRepository emailRepository)
    : IRequestHandler<GetEmailStatsQuery, EmailStatsSummary>
{
    public async Task<EmailStatsSummary> Handle(
        GetEmailStatsQuery request,
        CancellationToken cancellationToken)
    {
        return await emailRepository.GetStatsAsync(
            request.AccountEmail,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }
}
