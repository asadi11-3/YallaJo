using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace YallaJo.SharedKernel.Infrastructure.Data
{
    /// <summary>
    /// EF-specific typed UnitOfWork interface. Lives in Infrastructure, not Application,
    /// because it has a DbContext constraint.
    /// </summary>
    public interface IUnitOfWork<TContext> : IUnitOfWork
        where TContext : DbContext
    {
    }
}
