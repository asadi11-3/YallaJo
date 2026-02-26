using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

public sealed class InvoiceLineItem : BaseEntity
{
    private InvoiceLineItem() { } // EF Core

    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; } = 1;
    public Money UnitPrice { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }

    public InvoiceItem InvoiceItem { get; private set; } = default!;
}
