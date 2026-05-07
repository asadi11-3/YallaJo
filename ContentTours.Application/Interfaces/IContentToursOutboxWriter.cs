using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Application.Interfaces;

public interface IContentToursOutboxWriter
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
