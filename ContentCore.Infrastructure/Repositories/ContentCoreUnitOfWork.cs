using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentCore.Infrastructure.Repositories
{
    internal sealed class ContentCoreUnitOfWork(IUnitOfWork<ContentCoreDbContext> unitOfWork)
      : IContentCoreUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => unitOfWork.SaveChangesAsync(ct);
    }
}
