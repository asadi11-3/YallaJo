using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetTourBookingById;

/// <summary>
/// Fetch a single tour booking by id. Caller must be one of:
///   * the owning user (booking.UserId == currentUser.UserId)
///   * the provider that owns the tour (provider.OwnerUserId == currentUser.UserId)
///   * an admin (BookingFeatures.AdminBookingDashboard, AppAction.Read)
/// Authorization is enforced inside the handler (IDOR guard).
/// </summary>
/// <remarks>
/// The cache key is varied by viewer (and admin flag) so callers never receive
/// a cached projection that was produced for a different identity. The tag is
/// the booking id alone, so any state mutation on the booking invalidates the
/// entry for every viewer in a single <c>RemoveByTagAsync</c> call.
/// </remarks>
public sealed record GetTourBookingByIdQuery(Guid BookingId, Guid ViewerUserId, bool ViewerIsAdmin)
    : IQuery<TourBookingDetailDto>, ICacheableQuery
{
    public string CacheKey => $"booking:{BookingId:D}:viewer:{ViewerUserId:D}:admin:{(ViewerIsAdmin ? 1 : 0)}";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => [$"booking:{BookingId:D}"];
}
