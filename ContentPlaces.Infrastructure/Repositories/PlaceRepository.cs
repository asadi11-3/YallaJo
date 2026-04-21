using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

internal sealed class PlaceRepository(ContentPlacesDbContext context)
    : EfRepository<Place, Guid>(context), IPlaceRepository
{
    public Task<bool> HasActiveLinkedBusinessesAsync(Guid placeId, CancellationToken ct = default) =>
        context.PlaceBusinesses.AnyAsync(pb => pb.PlaceId == placeId, ct);
}
