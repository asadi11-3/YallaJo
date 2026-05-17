using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class InvoiceItemRepository(FinanceDbContext context)
    : EfRepository<InvoiceItem, Guid>(context), IInvoiceItemRepository
{
    private readonly FinanceDbContext _context = context;

    public Task<InvoiceItem?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default)
        => _context.InvoiceItems.FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, ct);

    public async Task<IReadOnlyList<InvoiceItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.InvoiceItems.Where(i => i.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<InvoiceItem>> GetOverdueAsync(DateOnly today, CancellationToken ct = default)
        => await _context.InvoiceItems
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.DueDate < today)
            .ToListAsync(ct);
}
