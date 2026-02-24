namespace YallaJo.SharedKernel.Application.Abstractions.Data
{
    /// <summary>
    /// Consumer-side idempotency store (Inbox pattern).
    /// Each consuming module registers a typed implementation (EfInboxStore&lt;TContext&gt;)
    /// backed by its own schema's InboxMessages table.
    ///
    /// Usage in an integration event handler:
    ///   if (await _inbox.HasBeenProcessedAsync(notification.MessageId, ct)) return;
    ///   // ... do work ...
    ///   _inbox.MarkAsProcessed(notification.MessageId);
    ///   await _unitOfWork.SaveChangesAsync(ct);   // persists inbox + business change atomically
    /// </summary>
    public interface IInboxStore
    {
        /// <summary>Returns true if this message has already been successfully processed.</summary>
        Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default);

        /// <summary>
        /// Tracks the message as processed in the EF change tracker.
        /// The record is persisted when the enclosing unit of work calls SaveChangesAsync.
        /// </summary>
        void MarkAsProcessed(Guid messageId);
    }
}
