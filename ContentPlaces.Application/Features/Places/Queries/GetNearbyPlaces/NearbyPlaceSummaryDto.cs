using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.Places.Queries.GetNearbyPlaces
{
    // شكل الداتا يلي راح ترجع لليوزر لما يطلب الأماكن القريبة
    public sealed record NearbyPlaceSummaryDto(
        Guid Id,
        string Name,
        string PrimaryImageUrl,
        double AverageRating,
        double DistanceKm,
        string Category);
}
