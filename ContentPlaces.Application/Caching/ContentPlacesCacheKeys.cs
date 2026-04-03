using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Caching
{
    public static class ContentPlacesCacheKeys
    {
        public static string BusinessListByPlace(Guid placeId, Guid? userId, bool isAdmin, int page, int pageSize) =>
            $"cp:biz:place:{placeId}:u:{userId}:a:{isAdmin}:p{page}:s{pageSize}";

        public static string Business(Guid id, Guid? userId, bool isAdmin) =>
        $"cp:biz:{id}:u:{userId}:a:{isAdmin}";
    }
}
