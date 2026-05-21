using System.Globalization;
using Finance.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finance.Infrastructure.Storage;

/// <summary>
/// Local filesystem-backed invoice PDF storage (dev/staging only). Stores files under
/// <c>App_Data/invoices/{userId}/{invoiceId}.pdf</c> relative to the configured base path.
/// </summary>
internal sealed class LocalFileInvoiceStorage(
    IOptions<LocalFileInvoiceStorageOptions> options,
    ILogger<LocalFileInvoiceStorage> logger)
    : IInvoiceStorage
{
    private readonly LocalFileInvoiceStorageOptions _options = options.Value;
    private readonly ILogger<LocalFileInvoiceStorage> _logger = logger;

    public async Task<string> StoreAsync(Guid userId, Guid invoiceId, byte[] pdfBytes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        var relativePath = string.Format(
            CultureInfo.InvariantCulture,
            "invoices/{0}/{1}.pdf",
            userId,
            invoiceId);

        var absolutePath = Path.Combine(_options.BasePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(absolutePath)!;
        Directory.CreateDirectory(dir);

        await File.WriteAllBytesAsync(absolutePath, pdfBytes, ct);
        _logger.LogDebug("Persisted invoice PDF to {Path} ({Bytes} bytes)", absolutePath, pdfBytes.Length);
        return relativePath;
    }

    public async Task<byte[]?> ReadAsync(string storagePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        var absolutePath = Path.Combine(_options.BasePath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(absolutePath, ct);
    }
}

public sealed class LocalFileInvoiceStorageOptions
{
    public const string SectionName = "Finance:Invoice:Storage";

    /// <summary>
    /// Absolute or relative base path where invoice PDFs are stored. Defaults to <c>App_Data</c>.
    /// </summary>
    public string BasePath { get; set; } = "App_Data";
}
