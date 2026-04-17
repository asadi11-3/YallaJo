using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Application.Queries.Dtos
{
    public sealed record RoleClaimDto(
      Guid Id,
      string ClaimType,
      string ClaimValue);
}
