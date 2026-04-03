using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories
{
    public sealed class BusinessRepository(ContentPlacesDbContext context)
        : EfRepository<Business, Guid>(context), IBusinessRepository
    {
        public async Task AddPlaceBusinessJunctionAsync(Guid placeId, Guid businessId, CancellationToken ct = default)
        {
            var junction = PlaceBusiness.Create(placeId, businessId);

            await context.Set<PlaceBusiness>().AddAsync(junction, ct);
        }

        public async Task RemovePlaceBusinessJunctionAsync(Guid placeId, Guid businessId, CancellationToken ct = default)
        {
            var junction = await context.Set<PlaceBusiness>()
                .FirstOrDefaultAsync(pb => pb.PlaceId == placeId && pb.BusinessId == businessId, ct);

            if (junction is not null)
            {
                context.Set<PlaceBusiness>().Remove(junction);
            }
        }
    }
}
