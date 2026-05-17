using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="RefundPolicy"/> aggregates.
/// </summary>
public interface IRefundPolicyRepository : IRepository<RefundPolicy, Guid>
{
    /// <summary>Returns the policy marked IsDefault=true, or null if none configured.</summary>
    Task<RefundPolicy?> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>Lists all active policies (IsActive=true).</summary>
    Task<IReadOnlyList<RefundPolicy>> GetAllActiveAsync(CancellationToken ct = default);
}
