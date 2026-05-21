using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class InvoiceRepository(FinanceDbContext context)
    : EfRepository<Invoice, Guid>(context), IInvoiceRepository
{
    private readonly FinanceDbContext _context = context;

    public async Task<IReadOnlyList<Invoice>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.Invoices
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetByProviderAsync(Guid providerId, CancellationToken ct = default)
        => await _context.Invoices
            .AsNoTracking()
            .Where(i => i.ProviderId == providerId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync(ct);

    public Task<Invoice?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.BookingId == bookingId, ct);

    public async Task<(IReadOnlyList<Invoice> Items, Guid? NextCursor)> GetByUserPageAsync(
        Guid userId,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default)
    {
        var size = pageSize;
        var query = _context.Invoices.AsNoTracking().Where(i => i.UserId == userId);
        if (afterId is not null)
        {
            query = query.Where(i => i.Id.CompareTo(afterId.Value) < 0);
        }

        var page = await query
            .OrderByDescending(i => i.Id)
            .Take(size + 1)
            .ToListAsync(ct);

        Guid? next = page.Count > size ? page[size - 1].Id : null;
        var items = page.Take(size).ToList();
        return (items, next);
    }

    public async Task<(IReadOnlyList<Invoice> Items, Guid? NextCursor)> GetByProviderPageAsync(
        Guid providerId,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default)
    {
        var size = pageSize;
        var query = _context.Invoices.AsNoTracking().Where(i => i.ProviderId == providerId);
        if (afterId is not null)
        {
            query = query.Where(i => i.Id.CompareTo(afterId.Value) < 0);
        }

        var page = await query
            .OrderByDescending(i => i.Id)
            .Take(size + 1)
            .ToListAsync(ct);

        Guid? next = page.Count > size ? page[size - 1].Id : null;
        var items = page.Take(size).ToList();
        return (items, next);
    }
}
