using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Application.Commands.VerifyEmail
{
    public sealed record VerifyEmailResult(
     Guid UserId,
     string AccessToken,
     string RefreshToken,
     DateTime RefreshTokenExpiresAt);
}
