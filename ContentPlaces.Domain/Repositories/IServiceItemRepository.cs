using ContentPlaces.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories
{
    public interface IServiceItemRepository : IRepository<ServiceItem, Guid>
    {
        Task<IReadOnlyList<ServiceItem>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct);
    }
}
