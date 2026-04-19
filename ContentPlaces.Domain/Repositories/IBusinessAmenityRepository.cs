using ContentPlaces.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories;

public interface IBusinessAmenityRepository : IReadRepository<BusinessAmenity, Guid>, IWriteRepository<BusinessAmenity, Guid>
{
}
