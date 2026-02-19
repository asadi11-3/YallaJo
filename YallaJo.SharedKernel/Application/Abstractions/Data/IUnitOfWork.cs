using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Data
{
    public interface IUnitOfWork<TContext> where TContext : DbContext
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
