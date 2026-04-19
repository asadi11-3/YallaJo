using ContentPlaces.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories;

public interface IBusinessStaffRepository : IReadRepository<BusinessStaff, Guid>, IWriteRepository<BusinessStaff, Guid>
{
}
