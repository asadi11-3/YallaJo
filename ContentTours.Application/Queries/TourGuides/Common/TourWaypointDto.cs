using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentTours.Application.Queries.TourGuides.Common
{
    public sealed record TourWaypointDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    int SortOrder,
    int? DurationMinutes,
    byte WaypointType
);

}
