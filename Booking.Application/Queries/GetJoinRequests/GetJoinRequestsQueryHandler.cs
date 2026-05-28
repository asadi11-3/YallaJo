using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetJoinRequests;

public sealed class GetJoinRequestsQueryHandler(
    IJoinRequestRepository joinRequestRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetJoinRequestsQuery, IReadOnlyList<JoinRequestDto>>
{
    public async Task<Result<IReadOnlyList<JoinRequestDto>>> Handle(GetJoinRequestsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        IReadOnlyList<JoinRequest> requests;

        if (request.MyRequestsOnly)
        {
            requests = await joinRequestRepository
                .GetByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (request.TourBookingId.HasValue)
        {
            requests = await joinRequestRepository
                .GetByBookingIdAsync(request.TourBookingId.Value, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            return Result.Success<IReadOnlyList<JoinRequestDto>>(Array.Empty<JoinRequestDto>());
        }

        var dtos = requests.Select(r => new JoinRequestDto(
            r.Id,
            r.TourBookingId,
            r.UserId,
            r.Status,
            r.ParticipantCount,
            r.Message,
            r.ExpiresAt,
            r.RespondedAt,
            r.ResponseMessage,
            r.ResultingBookingId,
            r.CreatedAt)).ToList();

        return Result.Success<IReadOnlyList<JoinRequestDto>>(dtos);
    }
}
