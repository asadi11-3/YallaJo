using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Application.Abstractions.Events
{
    /// <summary>
    /// Abstracts the dispatching of domain events so UnitOfWork
    /// doesn't depend directly on MediatR.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
    }
}
