using MediatR;
using Messaging.Application.Queries.Dtos;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetSupportTicketById;

public sealed record GetSupportTicketByIdQuery(Guid TicketId, Guid CallerUserId, bool IsAdmin) : IRequest<Result<SupportTicketDto>>;
