namespace YallaJo.Web.Areas.Provider.Models.Invoices;

// Mirrors the Finance InvoiceDto / InvoicePageDto returned by
// GET /api/v1/invoices/provider/my-invoices.

public sealed record ProviderInvoiceItemResponse(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record ProviderInvoiceResponse(
    Guid Id,
    Guid PaymentId,
    Guid BookingId,
    Guid UserId,
    Guid ProviderId,
    string InvoiceNumber,
    string Status,
    string Currency,
    decimal AmountSubtotal,
    decimal AmountTax,
    decimal AmountDiscount,
    decimal AmountTotal,
    DateTime IssuedAt,
    string BuyerName,
    string BuyerEmail,
    string SellerName,
    string? SellerTaxId,
    bool PdfAvailable,
    IReadOnlyList<ProviderInvoiceItemResponse> Items);

public sealed record ProviderInvoicePageResponse(
    IReadOnlyList<ProviderInvoiceResponse> Items,
    Guid? NextCursor);
