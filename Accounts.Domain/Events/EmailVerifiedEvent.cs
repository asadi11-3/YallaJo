using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events
{
    public sealed record EmailVerifiedEvent(Guid UserId, Guid EmailId, string EmailAddress) : DomainEventBase;
}
