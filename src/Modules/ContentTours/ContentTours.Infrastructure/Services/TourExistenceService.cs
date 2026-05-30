using ContentTours.Contracts.Tours;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentTours.Infrastructure.Services
{
    internal sealed class TourExistenceService(ContentToursDbContext dbContext)
    : ITourExistenceService
    {
        public async Task<TourExistenceStatus> GetStatusAsync(
            Guid tourId,
            CancellationToken cancellationToken = default)
        {
            var status = await dbContext.Tours
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.Id == tourId)
                .Select(t => (bool?)t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (status is null) return TourExistenceStatus.NotFound;
            return status.Value ? TourExistenceStatus.Deleted : TourExistenceStatus.Active;
        }
    }
}
