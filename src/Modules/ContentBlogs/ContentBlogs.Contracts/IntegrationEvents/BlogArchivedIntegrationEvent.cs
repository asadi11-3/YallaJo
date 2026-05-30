using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents
{
     public sealed record BlogArchivedIntegrationEvent(
     Guid BlogId,
     string Slug,
     DateTime ArchivedAt,
     decimal NewSitemapPriority = 0.3m) : IntegrationEventBase;
}
