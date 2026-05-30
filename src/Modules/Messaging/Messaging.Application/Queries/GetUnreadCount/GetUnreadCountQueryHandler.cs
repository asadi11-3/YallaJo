using MediatR;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetUnreadCount;

internal sealed class GetUnreadCountQueryHandler(
    INotificationRepository notificationRepository) : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await notificationRepository.GetUnreadCountByUserAsync(request.UserId, cancellationToken);
        return Result.Success(count);
    }
}
