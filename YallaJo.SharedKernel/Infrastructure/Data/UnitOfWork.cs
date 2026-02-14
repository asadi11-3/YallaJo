using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Entities;

namespace YallaJo.SharedKernel.Infrastructure.Data
{
   
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private readonly IMediator _mediator;
        public UnitOfWork(DbContext context,IMediator mediator)
        {
            _mediator = mediator;
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
           

            var domainEvents = _context.ChangeTracker
                .Entries<IAggregateRoot>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

           

            var result = await _context.SaveChangesAsync(ct);

            // ══════════════════════════════════════════
          

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, ct);
            }

            return result;
        }
    }
}
