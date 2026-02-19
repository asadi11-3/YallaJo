
using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events
{
    public sealed record EmailVerifiedEvent(Guid UserId, Guid EmailId, string EmailAddress) : DomainEventBase;
}
