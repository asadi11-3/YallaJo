using Auth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories
{
    public interface IExternalProviderRepository : IRepository<ExternalProvider, Guid>
    {
    }
}
