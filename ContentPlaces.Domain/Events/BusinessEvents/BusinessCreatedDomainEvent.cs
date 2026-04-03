using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events.BusinessEvents
{
    public sealed record BusinessCreatedDomainEvent
    (
        Guid Id,
        string Name,
        string Slug,
        Guid OwnerId,
        Guid? PlaceId

        ) : DomainEventBase;
    
}
