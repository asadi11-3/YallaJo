using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.CloseSupportTicket;

internal sealed class CloseSupportTicketCommandHandler(
    ISupportTicketRepository ticketRepository,
    IMessagingUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CloseSupportTicketCommand, Result>
{
    public async Task<Result> Handle(CloseSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure(new Error("SupportTicket.NotFound", "Ticket not found."), Outcome.NotFound);

        if (!request.IsAdmin && ticket.CreatedByUserId != request.CallerUserId)
            return Result.Failure(new Error("SupportTicket.OwnerMismatch", "You do not own this ticket."), Outcome.Forbidden);

        if (ticket.Status == TicketStatus.Closed)
            return Result.Failure(new Error("SupportTicket.AlreadyClosed", "Ticket is already closed."), Outcome.Conflict);

        ticket.Close(timeProvider);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
