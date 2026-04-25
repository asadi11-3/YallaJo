using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accounts.Application.Commands.UpdateProfile
{
    public sealed record UpdateProfileResult(
     string FirstName,
     string LastName);

}
