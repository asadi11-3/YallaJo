namespace YallaJo.SharedKernel.Domain.Event
{
    /// <summary>
    /// Marker interface for integration events. No MediatR dependency.
    /// Integration events are persisted to the outbox and dispatched via
    /// IntegrationEventNotification<T> by the OutboxProcessor.
    /// </summary>
    public interface IIntegrationEvent
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
    }

    public abstract record IntegrationEventBase : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.CreateVersion7();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
