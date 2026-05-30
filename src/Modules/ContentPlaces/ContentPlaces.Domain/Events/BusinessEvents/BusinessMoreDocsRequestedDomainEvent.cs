using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events.BusinessEvents
{
    public sealed record BusinessMoreDocsRequestedDomainEvent(
        Guid BusinessId,
        string Reason) : DomainEventBase;
}
