using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

/// <summary>
/// Read-side queries for <see cref="Invoice"/> aggregate.
/// </summary>
public interface IInvoiceRepository : IRepository<Invoice, Guid>
{
    /// <summary>Returns invoices owned by the given user (buyer), ordered by IssuedAt desc.</summary>
    Task<IReadOnlyList<Invoice>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns invoices issued for the given provider (seller), ordered by IssuedAt desc.</summary>
    Task<IReadOnlyList<Invoice>> GetByProviderAsync(Guid providerId, CancellationToken ct = default);

    /// <summary>Returns the single invoice tied to a booking, or null if none has been generated yet.</summary>
    Task<Invoice?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);

    /// <summary>Cursor-paginated buyer view (F-R10). Returns the next page anchored on Id.</summary>
    Task<(IReadOnlyList<Invoice> Items, Guid? NextCursor)> GetByUserPageAsync(
        Guid userId,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Cursor-paginated seller view (F-R10). Returns the next page anchored on Id.</summary>
    Task<(IReadOnlyList<Invoice> Items, Guid? NextCursor)> GetByProviderPageAsync(
        Guid providerId,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default);
}
