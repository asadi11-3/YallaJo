using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents
{
    public sealed record BlogUpdatedIntegrationEvent(
    Guid BlogId,
    string OldSlug,
    string NewSlug,
    IReadOnlyList<string> FieldsChanged,
    DateTime UpdatedAt) : IntegrationEventBase;
}
