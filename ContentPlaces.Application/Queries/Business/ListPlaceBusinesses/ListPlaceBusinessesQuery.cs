using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Business.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentPlaces.Application.Queries.Business.ListPlaceBusinesses
{
    public sealed record ListPlaceBusinessesQuery(
     Guid PlaceId,
     Guid? UserId,
     bool IsAdmin,
     int Page = 1,
     int PageSize = 20)
     : IQuery<PaginatedResult<BusinessSummaryDto>>, ICacheableQuery
    {
        public string CacheKey => ContentPlacesCacheKeys.BusinessListByPlace(PlaceId, UserId, IsAdmin, Page, PageSize);

        public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

        public IReadOnlyList<string> Tags =>
        [
            ContentPlacesCacheKeys.TagBusinesses,
            ContentPlacesCacheKeys.TagForPlaceBusinesses(PlaceId),
        ];
    }
}
