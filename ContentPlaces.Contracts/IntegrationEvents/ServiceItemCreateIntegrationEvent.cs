using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents
{
   public sealed record ServiceItemCreateIntegrationEvent(
       Guid ServiceItemId,
       Guid BusinessId,
       string Name,
       decimal Price,
       string Currency) : IntegrationEventBase;
}
