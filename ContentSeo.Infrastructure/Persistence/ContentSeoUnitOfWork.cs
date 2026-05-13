using ContentSeo.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentSeo.Infrastructure.Persistence;

/// <summary>
/// Module-scoped UoW. Forwards to the SharedKernel <see cref="UnitOfWork{TContext}"/>
/// so that domain events on tracked <c>IAggregateRoot</c> entries are dispatched
/// (and outbox rows written) within the SAME <c>SaveChangesAsync</c> transaction.
/// <para>
/// PW-1: previous implementation called <c>context.SaveChangesAsync</c> directly,
/// which skipped <see cref="YallaJo.SharedKernel.Application.Abstractions.Events.IDomainEventDispatcher"/>
/// dispatch and broke the outbox round-trip.
/// </para>
/// </summary>
internal sealed class ContentSeoUnitOfWork(IUnitOfWork<ContentSeoDbContext> inner) : IContentSeoUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => inner.SaveChangesAsync(ct);
}
