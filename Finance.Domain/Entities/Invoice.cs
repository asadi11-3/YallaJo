using Finance.Domain.Enums;
using Finance.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

/// <summary>
/// Aggregate root representing a customer-facing invoice generated for a completed payment.
/// </summary>
public sealed class Invoice : AuditableEntity, IAggregateRoot
{
    private readonly List<InvoiceItem> _items = [];

    private Invoice() { } // EF Core

    public Guid PaymentId { get; private set; }

    public Guid BookingId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProviderId { get; private set; }

    /// <summary>
    /// Human-readable invoice number in the form <c>INV-{YYYYMM}-{seq6}</c>.
    /// </summary>
    public string InvoiceNumber { get; private set; } = string.Empty;

    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Issued;

    public string Currency { get; private set; } = string.Empty;

    public Money AmountSubtotal { get; private set; } = default!;

    public Money AmountTax { get; private set; } = default!;

    public Money AmountDiscount { get; private set; } = default!;

    public Money AmountTotal { get; private set; } = default!;

    public DateTime IssuedAt { get; private set; }

    public string BuyerName { get; private set; } = string.Empty;

    public string BuyerEmail { get; private set; } = string.Empty;

    public string SellerName { get; private set; } = string.Empty;

    public string? SellerTaxId { get; private set; }

    /// <summary>
    /// Relative path (or blob key) where the rendered PDF is cached after the first download.
    /// Null until the PDF has been lazily rendered.
    /// </summary>
    public string? PdfStoragePath { get; private set; }

    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Factory that generates a new invoice for a completed payment. Raises
    /// <see cref="InvoiceGeneratedDomainEvent"/>. Per F-R1, amounts are rounded banker's-style.
    /// </summary>
    public static Invoice GenerateForPayment(
        Payment payment,
        string invoiceNumber,
        string buyerName,
        string buyerEmail,
        string sellerName,
        string? sellerTaxId,
        decimal taxAmount,
        decimal discountAmount,
        IReadOnlyList<InvoiceLineDraft> lineItems,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        ArgumentNullException.ThrowIfNull(lineItems);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (payment.PaymentType != PaymentType.Booking)
        {
            throw new InvalidOperationException("Only Booking-type payments can generate invoices.");
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException("Payment must be Completed to generate an invoice.");
        }

        if (lineItems.Count == 0)
        {
            throw new ArgumentException("At least one line item is required.", nameof(lineItems));
        }

        var currency = payment.Currency;
        var subtotalDecimal = 0m;
        foreach (var item in lineItems)
        {
            subtotalDecimal += item.UnitPrice * item.Quantity;
        }

        var tax = Math.Max(taxAmount, 0m);
        var discount = Math.Max(discountAmount, 0m);
        var total = subtotalDecimal + tax - discount;

        var invoice = new Invoice
        {
            PaymentId = payment.Id,
            BookingId = payment.BookingId ?? Guid.Empty,
            UserId = payment.UserId,
            ProviderId = payment.ProviderId,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Issued,
            Currency = currency,
            AmountSubtotal = new Money(Math.Round(subtotalDecimal, 2, MidpointRounding.ToEven), currency),
            AmountTax = new Money(Math.Round(tax, 2, MidpointRounding.ToEven), currency),
            AmountDiscount = new Money(Math.Round(discount, 2, MidpointRounding.ToEven), currency),
            AmountTotal = new Money(Math.Round(total, 2, MidpointRounding.ToEven), currency),
            IssuedAt = timeProvider.GetUtcNow().UtcDateTime,
            BuyerName = buyerName,
            BuyerEmail = buyerEmail,
            SellerName = sellerName,
            SellerTaxId = sellerTaxId,
        };

        foreach (var item in lineItems)
        {
            var unitPrice = new Money(Math.Round(item.UnitPrice, 2, MidpointRounding.ToEven), currency);
            var lineSubtotal = new Money(Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.ToEven), currency);
            invoice._items.Add(new InvoiceItem(invoice.Id, item.Description, item.Quantity, unitPrice, lineSubtotal));
        }

        invoice.AddDomainEvent(new InvoiceGeneratedDomainEvent(
            invoice.Id,
            invoice.BookingId,
            invoice.UserId,
            invoice.ProviderId,
            invoice.AmountTotal.Amount,
            invoice.AmountSubtotal.Amount,
            invoice.AmountTax.Amount,
            invoice.AmountDiscount.Amount,
            currency));

        return invoice;
    }

    /// <summary>
    /// Transitions the invoice to <see cref="InvoiceStatus.Cancelled"/> after a refund. Idempotent.
    /// </summary>
    public void CancelDueToRefund(string reason)
    {
        if (Status == InvoiceStatus.Cancelled)
        {
            return;
        }

        Status = InvoiceStatus.Cancelled;
        MarkUpdated();
    }

    /// <summary>
    /// Stamps the storage path of the rendered PDF after the first lazy download.
    /// </summary>
    public void MarkPdfRendered(string storagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        PdfStoragePath = storagePath;
        MarkUpdated();
    }
}

/// <summary>
/// Lightweight draft used when constructing <see cref="Invoice"/> line items via
/// <see cref="Invoice.GenerateForPayment"/>.
/// </summary>
public sealed record InvoiceLineDraft(string Description, int Quantity, decimal UnitPrice);
