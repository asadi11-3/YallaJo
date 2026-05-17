using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface IInvoiceItemRepository : IRepository<InvoiceItem, Guid>
{
    Task<InvoiceItem?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceItem>> GetOverdueAsync(DateOnly today, CancellationToken ct = default);
}
