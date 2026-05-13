using YallaJo.SharedKernel.Application.Authorization;

namespace Booking.Contracts.Authorization;

/// <summary>
/// Cross-module ownership probe for TourGuide aggregates.
/// Implementations must use a read-only, no-tracking projection that ignores
/// soft-delete query filters so that callers can distinguish missing from deleted rows.
/// Note: TourGuide uses an <c>IsActive</c> field rather than <c>IsDeleted</c>;
/// implementations are expected to map <c>IsActive == false</c> to
/// <see cref="EntityOwnershipResolution.IsDeleted"/> = <c>true</c>.
/// </summary>
public interface ITourGuideOwnershipService
{
    Task<EntityOwnershipResolution> GetTourGuideOwnershipAsync(Guid tourGuideId, CancellationToken ct = default);
}
