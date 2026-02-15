using MediatR;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Entities;

namespace YallaJo.SharedKernel.Infrastructure.Data
{
    public class UnitOfWork(DbContext context, IMediator mediator) : IUnitOfWork
    {
        private readonly DbContext _context = context;
        private readonly IMediator _mediator = mediator;

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var aggregateRoots = _context.ChangeTracker
                .Entries<IAggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = aggregateRoots
                .SelectMany(ar => ar.DomainEvents)
                .ToList();

            // Clear events BEFORE save to prevent re-dispatch on subsequent calls
            foreach (var aggregate in aggregateRoots)
            {
                aggregate.ClearDomainEvents();
            }

            var result = await _context.SaveChangesAsync(ct);

            // Dispatch events AFTER successful save
            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, ct);
            }

            return result;
        }
    }
}
