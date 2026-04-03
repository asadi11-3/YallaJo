using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace YallaJo.SharedKernel.Infrastructure.Data
{
 
    public interface IUnitOfWork<TContext> : IUnitOfWork
        where TContext : DbContext
    {
    }
}
