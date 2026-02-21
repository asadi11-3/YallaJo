using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace YallaJo.SharedKernel.Infrastructure.Data
{
    public sealed class UnitOfWork<TContext>(TContext context, IDomainEventDispatcher dispatcher)
        : IUnitOfWork<TContext>
        where TContext : DbContext
    {
        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var aggregates = context.ChangeTracker
                .Entries<IAggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = aggregates
                .SelectMany(a => a.DomainEvents)
                .ToList();

            foreach (var aggregate in aggregates)
                aggregate.ClearDomainEvents();

            var result = await context.SaveChangesAsync(ct);

            if (domainEvents.Count > 0)
                await dispatcher.DispatchAsync(domainEvents, ct);

            return result;
        }
    }
}
