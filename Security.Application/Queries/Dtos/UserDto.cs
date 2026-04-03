using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Application.Queries.Dtos
{
    public sealed record UserDto(
    Guid Id,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
}
