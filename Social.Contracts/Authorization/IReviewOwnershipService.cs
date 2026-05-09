using YallaJo.SharedKernel.Application.Authorization;

namespace Social.Contracts.Authorization;

/// <summary>
/// Cross-module ownership probe for Review aggregates.
/// Implementations must use a read-only, no-tracking projection that ignores
/// soft-delete query filters so that callers can distinguish missing from deleted rows.
/// </summary>
public interface IReviewOwnershipService
{
    Task<EntityOwnershipResolution> GetReviewOwnershipAsync(Guid reviewId, CancellationToken ct = default);
}
