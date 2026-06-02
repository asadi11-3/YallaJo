using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Common;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.ResolveSupportTicket;

internal sealed class ResolveSupportTicketCommandHandler(
    ISupportTicketRepository ticketRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider) : IRequestHandler<ResolveSupportTicketCommand, Result>
{
    public async Task<Result> Handle(ResolveSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure(new Error("SupportTicket.NotFound", "Ticket not found."), Outcome.NotFound);

        if (!RowVersionUtil.Equal(ticket.RowVersion, request.RowVersion))
            return Result.Failure(
                new Error("SupportTicket.ConcurrencyConflict", "This ticket was modified by another user. Please refresh and try again."),
                Outcome.Conflict);

        ticket.Resolve(request.ResolvedByUserId, request.ResolutionNotes, timeProvider);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(MessagingCacheKeys.SupportTicketTag(request.TicketId), cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(MessagingCacheKeys.SupportTicketsAdminTag, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
