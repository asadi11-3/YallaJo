
using MediatR;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Entities;

namespace YallaJo.SharedKernel.Infrastructure.Data;

public sealed class UnitOfWork<TContext>(TContext context, IMediator mediator)
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

       
        foreach (var ev in domainEvents)
            await mediator.Publish(ev, ct);

        return result;
    }
}