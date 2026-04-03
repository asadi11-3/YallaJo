using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Queries.Business.Common
{
    public sealed record BusinessSummaryDto(
        Guid Id,
        string Name,
        string Slug,
        string BusinessType,
        string Status,
        decimal AverageRating,
        int ReviewCount,
        DateTime CreatedAt);
}
