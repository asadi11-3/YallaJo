using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Repositories;

public interface IAnalyticsOutboxWriter
{
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
