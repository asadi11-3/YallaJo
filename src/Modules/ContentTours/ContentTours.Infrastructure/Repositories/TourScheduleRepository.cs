using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class TourScheduleRepository(ContentToursDbContext context)
    : EfEntityRepository<TourSchedule, Guid>(context), ITourScheduleRepository
{
}
