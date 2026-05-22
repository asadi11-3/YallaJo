using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Application.Interfaces;

/// <summary>
/// Writes integration events to the Auth module's outbox table for asynchronous delivery.
/// Implementation must use <see cref="YallaJo.SharedKernel.Infrastructure.Abstractions.Outbox.OutboxMessage"/>
/// factory to ensure proper type-registry lookup and immutability.
/// </summary>
public interface IAuthOutboxWriter
{
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
