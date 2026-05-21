using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.DeleteNotification;

public sealed record DeleteNotificationCommand(Guid NotificationId, Guid CallerUserId) : IRequest<Result>;
