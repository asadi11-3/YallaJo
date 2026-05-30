namespace Finance.Application.Queries.Dtos;

public sealed record InvoiceDto(
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
    IReadOnlyList<InvoiceItemDto> Items);

public sealed record InvoiceItemDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record InvoicePageDto(
    IReadOnlyList<InvoiceDto> Items,
    Guid? NextCursor);
