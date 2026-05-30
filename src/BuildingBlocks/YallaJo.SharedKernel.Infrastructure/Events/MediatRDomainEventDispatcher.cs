using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Events;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Infrastructure.Events
{
    /// <summary>
    /// Dispatches domain events via MediatR by wrapping them in DomainEventNotification&lt;T&gt;.
    /// Bridges the framework-agnostic IDomainEventDispatcher with MediatR's IPublisher.
    /// </summary>
    public sealed class MediatRDomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
    {
        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                var notification = Activator.CreateInstance(notificationType, domainEvent)!;
                await mediator.Publish((INotification)notification, ct);
            }
        }
    }
}
