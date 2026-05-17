using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="ProviderDocument"/> aggregates.
/// </summary>
public interface IProviderDocumentRepository : IRepository<ProviderDocument, Guid>
{
    /// <summary>Lists documents owned by a tour guide.</summary>
    Task<IReadOnlyList<ProviderDocument>> GetByTourGuideIdAsync(Guid tourGuideId, CancellationToken ct = default);

    /// <summary>Lists documents owned by a business.</summary>
    Task<IReadOnlyList<ProviderDocument>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default);

    /// <summary>
    /// Lists documents expiring on or before <paramref name="threshold"/> (UTC).
    /// Used by ProviderDocumentExpiryMonitor BG service.
    /// </summary>
    Task<IReadOnlyList<ProviderDocument>> GetExpiringAsync(DateTime threshold, CancellationToken ct = default);
}
