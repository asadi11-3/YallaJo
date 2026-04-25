using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Application.Commands.InviteUser
{
    public sealed record InviteUserResult(Guid UserId, Guid ProfileId);
}
