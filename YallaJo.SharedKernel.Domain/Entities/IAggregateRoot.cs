using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Domain.Entities
{
    public interface IAggregateRoot
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
    }
}
