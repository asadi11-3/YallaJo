using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Application.Features.ServiceItems.Events
{
   public sealed record ServiceItemCreateIntegrationEvent(
       Guid ServiceItemId,
       Guid BusinessId,
       string Name,
       decimal Price,
       string Currency) : IntegrationEventBase;
   public sealed record ServiceItemDeletedIntegrationEvent(
    Guid ServiceItemId,
    Guid BusinessId) : IntegrationEventBase;

}
