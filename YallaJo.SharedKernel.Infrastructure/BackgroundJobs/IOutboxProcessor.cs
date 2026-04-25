namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs
{
    public interface IOutboxProcessor
    {
        /// <summary>
        /// Processes a batch of pending outbox messages.
        /// Returns the number of messages processed in this cycle.
        /// Used by <see cref="CompositeOutboxProcessor"/> for adaptive polling.
        /// </summary>
        Task<int> ProcessOutboxMessagesAsync(CancellationToken ct = default);
    }
}
