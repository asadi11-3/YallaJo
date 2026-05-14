using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents
{
    public sealed record BlogUnpublishedIntegrationEvent(
    Guid BlogId,
    string Slug,
    DateTime UnpublishedAt) :IntegrationEventBase;
}
