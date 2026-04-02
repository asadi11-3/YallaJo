using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accounts.Application.Queries.GetProfile
{
    public sealed record GetProfileResult(
      Guid UserId,
      string FirstName,
      string LastName,
      string? DisplayName,
      string? AvatarUrl,
      string? PhoneNumber,
      DateOnly? DateOfBirth,
      string? Gender,
      string? Country,
      string? City,
      string? AddressLine,
      string Email);
}
