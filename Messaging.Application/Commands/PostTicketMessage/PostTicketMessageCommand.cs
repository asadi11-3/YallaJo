using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.PostTicketMessage;

public sealed record PostTicketMessageCommand(
    Guid TicketId,
    Guid AuthorUserId,
    string Body,
    bool IsInternal = false) : IRequest<Result>;
