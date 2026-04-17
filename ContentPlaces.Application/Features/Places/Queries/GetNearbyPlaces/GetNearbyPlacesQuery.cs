using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.Places.Queries.GetNearbyPlaces
{
    // الطلب يلي راح يجي من اليوزر لما يطلب الأماكن القريبة، بيحتوي على الإحداثيات ونصف القطر وعدد النتائج المطلوبة
    public sealed record GetNearbyPlacesQuery
    (
        double Lat,
        double Lng,
        double RadiusKm = 10, // نصف القطر الافتراضي 10 كم
        int PageSize = 10
    ) : IRequest<List<NearbyPlaceSummaryDto>>;
}
