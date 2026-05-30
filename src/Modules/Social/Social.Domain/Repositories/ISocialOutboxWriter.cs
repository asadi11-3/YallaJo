using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Repositories;

public interface ISocialOutboxWriter
{
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
