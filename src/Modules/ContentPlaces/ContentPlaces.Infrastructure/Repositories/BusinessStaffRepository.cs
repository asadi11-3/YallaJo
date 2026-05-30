using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

internal sealed class BusinessStaffRepository(ContentPlacesDbContext context)
    : EfEntityRepository<BusinessStaff, Guid>(context), IBusinessStaffRepository
{
    public Task<BusinessStaff?> GetByIdWithBusinessAsync(Guid id, bool asNoTracking = false, CancellationToken ct = default)
    {
        return GetAsync(
         filter: staff => staff.Id == id,
         include: query => query.Include(staff => staff.Business),
         asNoTracking: asNoTracking,
         ct: ct);
    }
}
