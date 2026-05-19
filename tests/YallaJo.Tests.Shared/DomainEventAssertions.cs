using FluentAssertions;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Event;

namespace YallaJo.Tests.Shared;

public static class DomainEventAssertions
{
    public static TEvent ShouldContainDomainEvent<TEvent>(this BaseEntity<Guid> entity)
        where TEvent : class, IDomainEvent
    {
        var domainEvent = entity.DomainEvents
            .OfType<TEvent>()
            .SingleOrDefault();

        domainEvent.Should().NotBeNull();
        return domainEvent!;
    }
}
