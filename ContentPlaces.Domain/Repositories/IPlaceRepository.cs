using ContentPlaces.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories;

public interface IPlaceRepository : IRepository<Place, Guid>
{
    Task<bool> HasActiveLinkedBusinessesAsync(Guid placeId, CancellationToken ct = default);
}
