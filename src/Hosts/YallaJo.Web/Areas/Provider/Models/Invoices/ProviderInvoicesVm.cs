namespace YallaJo.Web.Areas.Provider.Models.Invoices;

public sealed class ProviderInvoicesVm
{
    public IReadOnlyList<ProviderInvoiceRowVm> Invoices { get; init; } = [];

    public bool HasInvoices => Invoices.Count > 0;
}

public sealed record ProviderInvoiceRowVm(
    Guid Id,
    string InvoiceNumber,
    string Status,
    string Currency,
    decimal AmountTotal,
    DateTime IssuedAt,
    string BuyerName,
    bool PdfAvailable,
    int ItemCount);
