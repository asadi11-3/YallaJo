using Finance.Domain.Entities;
using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface IPaymentRepository : IRepository<Payment, Guid>
{
    Task<Payment?> GetByGatewayTransactionIdAsync(string transactionId, CancellationToken ct = default);

    Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<Payment>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);

    /// <summary>
    /// Cursor page of payments owned by the caller (User self-filter).
    /// Ordered newest-first by Id (Guid v7) for stable cursor pagination.
    /// </summary>
    Task<(IReadOnlyList<Payment> Items, Guid? NextCursor)> GetMyPaymentsPageAsync(
        Guid userId,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Cursor page of payments for an admin dashboard with optional filters.
    /// </summary>
    Task<(IReadOnlyList<Payment> Items, Guid? NextCursor, int? TotalCount)> GetAdminPageAsync(
        Guid? userId,
        Guid? providerId,
        PaymentStatus? status,
        PaymentType? type,
        Guid? afterId,
        int pageSize,
        bool countTotal,
        CancellationToken ct = default);

    /// <summary>
    /// All Refund-type payments stuck in Failed older than the given UTC threshold with RetryCount &lt; maxRetries.
    /// Used by T5 RefundRetryService.
    /// </summary>
    Task<IReadOnlyList<Payment>> GetPendingRefundsOlderThanAsync(
        DateTime olderThanUtc,
        int maxRetries,
        int batchSize,
        CancellationToken ct = default);

    /// <summary>
    /// All Booking-type payments with Status=Completed and EscrowReleaseEligibleAt &lt;= asOf,
    /// not yet linked to a PayoutItem. Used by T4/T5 PayoutBatchingService trigger.
    /// </summary>
    Task<IReadOnlyList<Payment>> GetEscrowReleaseEligibleAsync(
        DateTime asOfUtc,
        CancellationToken ct = default);
}
