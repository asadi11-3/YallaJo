using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Application.Interfaces;

public interface IContentCoreOutboxWriter
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
