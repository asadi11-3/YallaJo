using ContentPlaces.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories;

public interface IBusinessStaffRepository : IReadRepository<BusinessStaff, Guid>, IWriteRepository<BusinessStaff, Guid>
{
    Task<BusinessStaff?> GetByIdWithBusinessAsync(
    Guid id,
    bool asNoTracking = false,
    CancellationToken ct = default);
}
