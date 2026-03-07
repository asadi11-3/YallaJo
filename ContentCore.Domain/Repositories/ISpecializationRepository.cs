using ContentCore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories
{
    public interface ISpecializationRepository : IReadRepository<Specialization,Guid>, IWriteRepository<Specialization,Guid>
    {

    }
}
