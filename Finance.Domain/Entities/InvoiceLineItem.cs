using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class InvoiceLineItem : BaseEntity
{
    private InvoiceLineItem() { } // EF Core

    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal Amount { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }

    public InvoiceItem InvoiceItem { get; private set; } = default!;
}
