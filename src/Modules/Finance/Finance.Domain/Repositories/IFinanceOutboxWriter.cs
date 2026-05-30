using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Repositories;

/// <summary>
/// Writes integration events to the Finance module's outbox table for asynchronous delivery.
/// Implementation must use <see cref="YallaJo.SharedKernel.Infrastructure.Abstractions.Outbox.OutboxMessage"/>
/// factory to ensure proper type-registry lookup and immutability.
/// </summary>
public interface IFinanceOutboxWriter
{
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
