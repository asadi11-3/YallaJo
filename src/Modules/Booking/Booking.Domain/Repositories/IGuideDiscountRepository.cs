using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="GuideDiscount"/> aggregates.
/// </summary>
public interface IGuideDiscountRepository : IRepository<GuideDiscount, Guid>
{
    /// <summary>Gets active discounts for a specific guide (and optional tour).</summary>
    Task<IReadOnlyList<GuideDiscount>> GetActiveByGuideAsync(Guid guideUserId, Guid? tourId, DateTime utcNow, CancellationToken ct = default);

    /// <summary>Gets all discounts created by a specific guide.</summary>
    Task<IReadOnlyList<GuideDiscount>> GetByGuideUserIdAsync(Guid guideUserId, CancellationToken ct = default);
}
