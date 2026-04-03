using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Application.Commands.Login
{
    public sealed record LoginResult(
     Guid UserId,
     string AccessToken,
     string RefreshToken,
     DateTime RefreshTokenExpiresAt);
}
