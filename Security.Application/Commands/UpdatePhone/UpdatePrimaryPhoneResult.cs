using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Application.Commands.UpdatePhone
{
    public sealed record UpdatePrimaryPhoneResult(bool Success, string PhoneNumber);
}
