using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.CloseSupportTicket;

public sealed record CloseSupportTicketCommand(Guid TicketId, Guid CallerUserId, bool IsAdmin) : IRequest<Result>;
