using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Business.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Business.GetBusinessById
{
    public sealed record GetBusinessByIdQuery(
    Guid Id,
    Guid? UserId,
    bool IsAdmin)
    : IQuery<BusinessDetailDto>, ICacheableQuery
    {
        public string CacheKey => ContentPlacesCacheKeys.Business(Id, UserId, IsAdmin);

        public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

        public IReadOnlyList<string> Tags => ["businesses", $"biz:{Id}"];
    }
}
