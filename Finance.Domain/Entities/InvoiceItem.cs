using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

public sealed class InvoiceItem : AuditableEntity, IAggregateRoot
{
    private readonly List<InvoiceLineItem> _invoiceLineItems = [];

    private InvoiceItem() { } // EF Core

    public Guid UserId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public Money SubTotal { get; private set; } = default!;
    public Money TaxAmount { get; private set; } = default!;
    public Money TotalAmount { get; private set; } = default!;
    public string Currency { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<InvoiceLineItem> InvoiceLineItems => _invoiceLineItems.AsReadOnly();
}
