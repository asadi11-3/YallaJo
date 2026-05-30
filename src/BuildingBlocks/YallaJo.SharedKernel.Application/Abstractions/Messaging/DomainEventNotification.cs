using MediatR;
using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging
{
    /// <summary>
    /// Wraps a domain event as a MediatR INotification.
    /// Keeps IDomainEvent free of MediatR dependencies (it lives in Domain).
    /// Domain event handlers implement: INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;
    /// </summary>
    public sealed record DomainEventNotification<TEvent>(TEvent Event) : INotification
        where TEvent : IDomainEvent;
}
