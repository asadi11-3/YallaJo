using Finance.Domain.Entities;

namespace Finance.Application.Interfaces;

/// <summary>
/// Renders an invoice aggregate to a PDF byte stream. Pure formatter — no I/O.
/// </summary>
public interface IInvoicePdfRenderer
{
    /// <summary>
    /// Renders the invoice (with optional cancellation watermark) to PDF bytes.
    /// </summary>
    byte[] Render(Invoice invoice);
}
