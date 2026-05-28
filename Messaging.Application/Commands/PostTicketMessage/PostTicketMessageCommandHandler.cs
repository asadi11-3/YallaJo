using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.PostTicketMessage;

internal sealed class PostTicketMessageCommandHandler(
    ISupportTicketRepository ticketRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider) : IRequestHandler<PostTicketMessageCommand, Result>
{
    public async Task<Result> Handle(PostTicketMessageCommand request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure(new Error("SupportTicket.NotFound", "Ticket not found."), Outcome.NotFound);

        // Ownership: must be creator or admin (admin is determined at endpoint)
        if (ticket.CreatedByUserId != request.AuthorUserId && !request.IsInternal)
            return Result.Failure(new Error("SupportTicket.OwnerMismatch", "You do not own this ticket."), Outcome.Forbidden);

        if (string.IsNullOrWhiteSpace(request.Body))
            return Result.Failure(new Error("SupportTicket.MessageRequired", "Message body is required."), Outcome.UnprocessableEntity);

        ticket.AddMessage(request.AuthorUserId, request.Body, request.IsInternal, timeProvider);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(MessagingCacheKeys.SupportTicketTag(request.TicketId), cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(MessagingCacheKeys.TicketMessagesTag(request.TicketId), cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
