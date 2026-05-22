using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.AssignSupportTicket;

internal sealed class AssignSupportTicketCommandHandler(
    ISupportTicketRepository ticketRepository,
    IMessagingUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<AssignSupportTicketCommand, Result>
{
    public async Task<Result> Handle(AssignSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure(new Error("SupportTicket.NotFound", "Ticket not found."), Outcome.NotFound);

        ticket.AssignTo(request.AdminUserId, request.AssignedByUserId, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
