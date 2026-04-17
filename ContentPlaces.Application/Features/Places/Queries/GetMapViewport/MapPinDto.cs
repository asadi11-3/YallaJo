using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Features.Places.Queries.GetMapViewport
{
    // شكل الداتا يلي راح ترجع لليوزر لما يطلب الأماكن ضمن نطاق معين
    public sealed record MapPinDto
     (
         Guid Id,
         string Name,
         double Lat,
         double Lng,
         string PrimaryImageUrl,
         double AverageRating,
         int TourCount
     );
}
