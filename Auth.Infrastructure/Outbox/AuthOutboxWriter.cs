using Auth.Application.Interfaces;
using Auth.Infrastructure.Persistence;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Infrastructure.Outbox;

/// <summary>
/// Persists integration events to the Auth module's <c>OutboxMessages</c> table within the
/// active <see cref="AuthDbContext"/> change tracker so the outbox row commits atomically
/// with the surrounding business change (single SaveChangesAsync).
/// </summary>
internal sealed class AuthOutboxWriter(AuthDbContext dbContext) : IAuthOutboxWriter
{
    public Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        return Task.CompletedTask;
    }
}
