using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.CreateSupportTicket;

internal sealed class CreateSupportTicketCommandHandler(
    ISupportTicketRepository ticketRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider) : IRequestHandler<CreateSupportTicketCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = SupportTicket.Create(
            request.CreatedByUserId, request.Category, request.Subject, request.Body, timeProvider);

        await ticketRepository.AddAsync(ticket, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(MessagingCacheKeys.SupportTicketsTag(request.CreatedByUserId), cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(MessagingCacheKeys.SupportTicketsAdminTag, cancellationToken).ConfigureAwait(false);

        return Result.Success(ticket.Id);
    }
}
