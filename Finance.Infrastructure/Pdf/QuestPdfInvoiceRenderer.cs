using System.Globalization;
using Finance.Application.Interfaces;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Finance.Infrastructure.Pdf;

/// <summary>
/// QuestPDF-backed invoice renderer (T3, F-R8). License is configured in DI bootstrap.
/// </summary>
internal sealed class QuestPdfInvoiceRenderer : IInvoicePdfRenderer
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public byte[] Render(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("YallaJo").FontSize(22).SemiBold();
                        row.RelativeItem().AlignRight().Text("INVOICE").FontSize(22).SemiBold();
                    });

                    if (invoice.Status == InvoiceStatus.Cancelled)
                    {
                        col.Item().PaddingTop(4).Text("CANCELLED").FontColor(Colors.Red.Medium).FontSize(16).SemiBold();
                    }

                    col.Item().PaddingTop(8).Text($"Invoice #: {invoice.InvoiceNumber}");
                    col.Item().Text($"Issued: {invoice.IssuedAt:yyyy-MM-dd}");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bill To").SemiBold();
                            c.Item().Text(invoice.BuyerName);
                            c.Item().Text(invoice.BuyerEmail);
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Seller").SemiBold();
                            c.Item().Text(invoice.SellerName);
                            if (!string.IsNullOrWhiteSpace(invoice.SellerTaxId))
                            {
                                c.Item().Text($"Tax ID: {invoice.SellerTaxId}");
                            }
                        });
                    });

                    col.Item().LineHorizontal(0.5f);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Description").SemiBold();
                            h.Cell().AlignRight().Text("Qty").SemiBold();
                            h.Cell().AlignRight().Text("Unit").SemiBold();
                            h.Cell().AlignRight().Text("Subtotal").SemiBold();
                        });

                        foreach (var item in invoice.Items)
                        {
                            table.Cell().Text(item.Description);
                            table.Cell().AlignRight().Text(item.Quantity.ToString(Culture));
                            table.Cell().AlignRight().Text($"{item.UnitPrice.Amount.ToString("F2", Culture)} {invoice.Currency}");
                            table.Cell().AlignRight().Text($"{item.Subtotal.Amount.ToString("F2", Culture)} {invoice.Currency}");
                        }
                    });

                    col.Item().AlignRight().Column(c =>
                    {
                        c.Item().Text($"Subtotal: {invoice.AmountSubtotal.Amount.ToString("F2", Culture)} {invoice.Currency}");
                        c.Item().Text($"Tax: {invoice.AmountTax.Amount.ToString("F2", Culture)} {invoice.Currency}");
                        c.Item().Text($"Discount: -{invoice.AmountDiscount.Amount.ToString("F2", Culture)} {invoice.Currency}");
                        c.Item().Text($"Total: {invoice.AmountTotal.Amount.ToString("F2", Culture)} {invoice.Currency}")
                            .FontSize(14).SemiBold();
                    });
                });

                page.Footer().AlignCenter().Text("Thank you for booking with YallaJo.").FontSize(9);
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Activates the QuestPDF Community License (free for OSS / small biz). Must be called once
    /// at process startup before any document is rendered.
    /// </summary>
    public static void ActivateCommunityLicense()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
