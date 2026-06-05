namespace YallaJo.Web.Areas.Accounts.Models.Invoices;

public sealed record InvoiceItemResponse(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record InvoiceResponse(
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
    IReadOnlyList<InvoiceItemResponse> Items);

public sealed record InvoicePageResponse(
    IReadOnlyList<InvoiceResponse> Items,
    Guid? NextCursor);
