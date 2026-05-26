using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.AssignSupportTicket;

internal sealed class AssignSupportTicketCommandHandler(
    ISupportTicketRepository ticketRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider) : IRequestHandler<AssignSupportTicketCommand, Result>
{
    public async Task<Result> Handle(AssignSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure(new Error("SupportTicket.NotFound", "Ticket not found."), Outcome.NotFound);

        ticket.AssignTo(request.AdminUserId, request.AssignedByUserId, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(MessagingCacheKeys.SupportTicketTag(request.TicketId), cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(MessagingCacheKeys.SupportTicketsAdminTag, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
