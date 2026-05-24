namespace Booking.Application.Queries.JoinRequest;

internal static class JoinRequestMapper
{
    public static JoinRequestDto ToDto(Booking.Domain.Entities.JoinRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new JoinRequestDto(
            Id: request.Id,
            TourBookingId: request.TourBookingId,
            UserId: request.UserId,
            ParticipantCount: request.ParticipantCount,
            Message: request.Message,
            Status: request.Status,
            RespondedAt: request.RespondedAt,
            ResponseMessage: request.ResponseMessage,
            RowVersion: Convert.ToBase64String(request.RowVersion ?? []));
    }
}
