
using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events
{
   public sealed record UserRegisteredEvent(Guid UserId, string Email) : DomainEventBase;
}
