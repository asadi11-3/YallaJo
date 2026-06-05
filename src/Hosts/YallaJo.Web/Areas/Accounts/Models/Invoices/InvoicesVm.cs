namespace YallaJo.Web.Areas.Accounts.Models.Invoices;

public sealed class InvoicesVm
{
    public IReadOnlyList<InvoiceRowVm> Invoices { get; init; } = [];

    public bool HasInvoices => Invoices.Count > 0;
}

public sealed record InvoiceRowVm(
    Guid Id,
    string InvoiceNumber,
    string Status,
    string Currency,
    decimal AmountTotal,
    DateTime IssuedAt,
    string SellerName,
    bool PdfAvailable,
    int ItemCount);
