using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Application.Commands.AcceptInvite
{
    public sealed record AcceptInviteResult(Guid UserId);
}
