using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Application.Commands.CreateRole
{
    public sealed record CreateRoleResult(Guid Id, string Name);
}
