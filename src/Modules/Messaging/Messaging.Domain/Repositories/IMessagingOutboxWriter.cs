using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Repositories;

public interface IMessagingOutboxWriter
{
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
