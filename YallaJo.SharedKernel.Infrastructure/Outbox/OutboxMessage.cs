using System.Text.Json;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Abstractions.Integration;

namespace YallaJo.SharedKernel.Infrastructure.Outbox
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; private set; }
        public string Type { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public DateTime OccurredOnUtc { get; private set; }
        public DateTime? ProcessedOnUtc { get; private set; }
        public string? Error { get; private set; }
        public int RetryCount { get; private set; }
        public DateTime? LockedUntil { get; private set; }

        /// <summary>
        /// W3C traceparent + tracestate serialised as JSON by <see cref="TraceContextHelpers.Capture"/>.
        /// Null for rows written before PR 4 — processor starts a fresh root span in that case.
        /// </summary>
        public string? TraceContext { get; private set; }

        /// <summary>
        /// Explicit message status. Defaults to <see cref="OutboxMessageStatus.Pending"/>.
        /// Existing rows (written before PR 5) have the column default of 0 = Pending,
        /// which is correct — they will be picked up and processed normally.
        /// </summary>
        public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;

        private OutboxMessage() { }

        public static OutboxMessage Create(IIntegrationEvent integrationEvent)
        {
            return new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = IntegrationEventTypeRegistry.GetName(integrationEvent.GetType()),
                Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
                OccurredOnUtc = integrationEvent.OccurredOn,
                ProcessedOnUtc = null,
                Error = null,
                RetryCount = 0,
                LockedUntil = null,
                Status = OutboxMessageStatus.Pending,
                TraceContext = TraceContextHelpers.Capture(),
            };
        }

        public void Lock(DateTime until)
        {
            LockedUntil = until;
            Status = OutboxMessageStatus.Processing;
        }

        public void MarkAsProcessed()
        {
            ProcessedOnUtc = DateTime.UtcNow;
            Status = OutboxMessageStatus.Processed;
        }

        /// <summary>Marks as processed at a specific time (tests + cleanup verification).</summary>
        public void MarkAsProcessedAt(DateTime utcTime)
        {
            ProcessedOnUtc = utcTime;
            Status = OutboxMessageStatus.Processed;
        }

        public void MarkAsFailed(string error)
        {
            Error = error;
            RetryCount++;
            Status = RetryCount >= OutboxConstants.MaxRetryCount
                ? OutboxMessageStatus.Dead
                : OutboxMessageStatus.Failed;
        }

        /// <summary>
        /// Creates a fresh replay clone of this message.
        /// The clone has a new Id, zeroed RetryCount + Error, and a fresh OccurredOnUtc
        /// so the outbox processor picks it up immediately.
        /// The original dead-lettered row is NOT modified — it stays as an audit record.
        /// </summary>
        public OutboxMessage CreateReplayCopy()
        {
            return new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = Type,
                Content = Content,
                OccurredOnUtc = DateTime.UtcNow,
                ProcessedOnUtc = null,
                Error = null,
                RetryCount = 0,
                LockedUntil = null,
                Status = OutboxMessageStatus.Pending,
                // Capture current trace context for the replay span (fresh from ops request)
                TraceContext = TraceContextHelpers.Capture(),
            };
        }
    }
}
