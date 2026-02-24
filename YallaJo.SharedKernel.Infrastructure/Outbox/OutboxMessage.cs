using System.Text.Json;
using YallaJo.SharedKernel.Domain.Event;

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

        private OutboxMessage() { }

        public static OutboxMessage Create(IIntegrationEvent integrationEvent)
        {
            return new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = integrationEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
                OccurredOnUtc = integrationEvent.OccurredOn,
                ProcessedOnUtc = null,
                Error = null,
                RetryCount = 0,
                LockedUntil = null
            };
        }

        public void Lock(DateTime until) => LockedUntil = until;

        public void MarkAsProcessed() => ProcessedOnUtc = DateTime.UtcNow;

        public void MarkAsFailed(string error)
        {
            Error = error;
            RetryCount++;
        }
    }
}
