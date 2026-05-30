using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Accounts.Infrastructure.Repositories;

internal sealed class AccountsOutboxWriter(AccountsDbContext context) : IAccountsOutboxWriter
{
    public Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var message = OutboxMessage.Create(integrationEvent);
        context.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }
}
