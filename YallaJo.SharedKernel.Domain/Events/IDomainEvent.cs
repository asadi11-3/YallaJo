namespace YallaJo.SharedKernel.Domain.Event
{
    /// <summary>
    /// Marker interface for domain events. Deliberately has NO MediatR dependency —
    /// the domain layer must remain framework-agnostic.
    /// Handlers receive events via DomainEventNotification<T> (in Application layer).
    /// </summary>
    public interface IDomainEvent
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
    }

    public abstract record DomainEventBase : IDomainEvent
    {
        public Guid EventId { get; } = Guid.CreateVersion7();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
