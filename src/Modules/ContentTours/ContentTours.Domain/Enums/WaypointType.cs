using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentTours.Domain.Enums
{
    public enum WaypointType : byte
    {
        Start = 0,
        Stop = 1,
        Meal = 2,
        Photo = 3,
        LandMark = 4,
        RestStop = 5,
        End = 6
    }
}
