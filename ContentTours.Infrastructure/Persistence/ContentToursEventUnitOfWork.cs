using ContentTours.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentTours.Infrastructure.Persistence;

/// <summary>
/// Event-dispatching unit of work for ContentTours.
/// Wraps <see cref="IUnitOfWork{ContentToursDbContext}"/> which dispatches domain events
/// from IAggregateRoot entries before SaveChanges.
/// </summary>
internal sealed class ContentToursEventUnitOfWork(
    IUnitOfWork<ContentToursDbContext> inner) : IContentToursEventUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
