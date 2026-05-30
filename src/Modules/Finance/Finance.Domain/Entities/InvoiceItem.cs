using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

/// <summary>
/// Line item belonging to an <see cref="Invoice"/>. Stays as a <see cref="BaseEntity"/> per the
/// Finance sprint pre-work conventions (junction/line entities never raise domain events).
/// </summary>
public sealed class InvoiceItem : BaseEntity
{
    private InvoiceItem() { } // EF Core

    internal InvoiceItem(
        Guid invoiceId,
        string description,
        int quantity,
        Money unitPrice,
        Money subtotal)
    {
        InvoiceId = invoiceId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Subtotal = subtotal;
    }

    public Guid InvoiceId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public int Quantity { get; private set; } = 1;

    public Money UnitPrice { get; private set; } = default!;

    public Money Subtotal { get; private set; } = default!;
}
