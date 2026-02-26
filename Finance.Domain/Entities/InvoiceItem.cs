using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class InvoiceItem : AuditableEntity, IAggregateRoot
{
    private readonly List<InvoiceLineItem> _invoiceLineItems = [];

    private InvoiceItem() { } // EF Core

    public Guid UserId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<InvoiceLineItem> InvoiceLineItems => _invoiceLineItems.AsReadOnly();
}
