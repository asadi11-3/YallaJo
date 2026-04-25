using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Application.Interfaces.SessionRevocation
{
    public sealed record SessionRevocationOutcome(
     int SessionsRevoked,
     int RefreshTokensRevoked);
}
