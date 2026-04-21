using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentCore.Infrastructure.Repositories
{
    internal sealed class ContentCoreUnitOfWork(IUnitOfWork<ContentCoreDbContext> unitOfWork)
      : IContentCoreUnitOfWork
    {
        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                return await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new DbUpdateConcurrencyException(
                "A concurrency conflict occurred. Please refresh and try again.", ex);
            }
        }
    }
}
