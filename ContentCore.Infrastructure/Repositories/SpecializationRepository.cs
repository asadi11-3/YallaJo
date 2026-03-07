using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories
{
    internal class SpecializationRepository(ContentCoreDbContext context) : EfEntityRepository<Specialization,Guid>(context), ISpecializationRepository
    {
    }
}
