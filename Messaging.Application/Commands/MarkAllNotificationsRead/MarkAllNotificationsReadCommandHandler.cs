using MediatR;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.MarkAllNotificationsRead;

internal sealed class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository) : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        await notificationRepository.MarkAllAsReadByUserAsync(request.UserId, cancellationToken);
        return Result.Success();
    }
}