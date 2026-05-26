using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.BatchDeleteNotifications;

public sealed record BatchDeleteNotificationsCommand(IReadOnlyList<Guid> NotificationIds, Guid CallerUserId) : IRequest<Result>;
