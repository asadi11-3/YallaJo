using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Presentation.ServiceItems
{
    public sealed record UpdateServiceItemRequest
        (
        string Name,
        decimal Price,
        int DurationMinutes,
        int MaxCapacity,
        string Currency,
        int SortOrder
        );
}
