using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.ResolveSupportTicket;

internal sealed class ResolveSupportTicketCommandHandler(
    ISupportTicketRepository ticketRepository,
    IMessagingUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ResolveSupportTicketCommand, Result>
{
    public async Task<Result> Handle(ResolveSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdWithMessagesAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure(new Error("SupportTicket.NotFound", "Ticket not found."), Outcome.NotFound);

        ticket.Resolve(request.ResolvedByUserId, request.ResolutionNotes, timeProvider);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
