using MediatR;
using Messaging.Application.Queries.Dtos;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetNotificationById;

public sealed record GetNotificationByIdQuery(Guid Id, Guid CallerUserId) : IRequest<Result<NotificationDto>>;
