using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents
{
    public sealed record BlogDeletedIntegrationEvent(
    Guid BlogId,
    string Slug,
    DateTime DeletedAt) :IntegrationEventBase;
}
