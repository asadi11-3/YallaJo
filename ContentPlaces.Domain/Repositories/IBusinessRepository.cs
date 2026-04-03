using ContentPlaces.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories
{
    public interface IBusinessRepository : IRepository<Business, Guid>
    {
        Task AddPlaceBusinessJunctionAsync(Guid placeId, Guid businessId, CancellationToken ct = default);
        Task RemovePlaceBusinessJunctionAsync(Guid placeId, Guid businessId, CancellationToken ct = default);
    }
}
