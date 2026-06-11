using MediatR;
using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetSupportTicketStatusCounts;

internal sealed class GetSupportTicketStatusCountsQueryHandler(
    ISupportTicketRepository ticketRepository,
    ILogger<GetSupportTicketStatusCountsQueryHandler> logger)
    : IRequestHandler<GetSupportTicketStatusCountsQuery, Result<SupportTicketStatusCountsDto>>
{
    public async Task<Result<SupportTicketStatusCountsDto>> Handle(GetSupportTicketStatusCountsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var counts = await ticketRepository.GetStatusCountsAsync(cancellationToken).ConfigureAwait(false);

            var dto = new SupportTicketStatusCountsDto(
                Open: counts.GetValueOrDefault(TicketStatus.Open),
                Assigned: counts.GetValueOrDefault(TicketStatus.Assigned),
                InProgress: counts.GetValueOrDefault(TicketStatus.InProgress),
                AwaitingUser: counts.GetValueOrDefault(TicketStatus.AwaitingUser),
                Resolved: counts.GetValueOrDefault(TicketStatus.Resolved),
                Closed: counts.GetValueOrDefault(TicketStatus.Closed));

            logger.LogDebug("Computed support ticket status counts for admin queue tabs");

            return Result.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<SupportTicketStatusCountsDto>(
                new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
