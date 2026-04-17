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
    public sealed class ServiceItemRepository : EfRepository<ServiceItem, Guid>, IServiceItemRepository
    {
        private readonly ContentPlacesDbContext _context;
        public ServiceItemRepository(ContentPlacesDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ServiceItem>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct)
        {
            return await _context.ServiceItems
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId)
                .ToListAsync(ct);
        }
    }
}
