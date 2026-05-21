namespace Finance.Application.Interfaces;

/// <summary>
/// Lightweight blob-style storage for rendered invoice PDFs. Implementations may use the local
/// filesystem (dev) or an Azure Blob container (prod).
/// </summary>
public interface IInvoiceStorage
{
    /// <summary>
    /// Persists the PDF bytes and returns the relative storage path (e.g.
    /// <c>invoices/{userId}/{invoiceId}.pdf</c>) used to retrieve them later.
    /// </summary>
    Task<string> StoreAsync(Guid userId, Guid invoiceId, byte[] pdfBytes, CancellationToken ct = default);

    /// <summary>
    /// Reads the PDF bytes for the given storage path. Returns <c>null</c> if not found.
    /// </summary>
    Task<byte[]?> ReadAsync(string storagePath, CancellationToken ct = default);
}
