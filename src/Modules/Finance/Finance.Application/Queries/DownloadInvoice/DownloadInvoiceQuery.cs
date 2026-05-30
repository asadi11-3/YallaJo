using Finance.Application.Interfaces;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.DownloadInvoice;

public sealed record DownloadInvoiceQuery(
    Guid InvoiceId,
    Guid CallerUserId,
    bool CallerIsAdmin,
    Guid? CallerProviderId) : IRequest<Result<DownloadInvoiceResult>>;

public sealed record DownloadInvoiceResult(byte[] PdfBytes, string FileName, string ContentType);

public sealed class DownloadInvoiceQueryHandler(
    IInvoiceRepository invoiceRepository,
    IInvoicePdfRenderer pdfRenderer,
    IInvoiceStorage storage,
    IFinanceUnitOfWork unitOfWork,
    ILogger<DownloadInvoiceQueryHandler> logger)
    : IRequestHandler<DownloadInvoiceQuery, Result<DownloadInvoiceResult>>
{
    private readonly IInvoiceRepository _invoiceRepository = invoiceRepository;
    private readonly IInvoicePdfRenderer _pdfRenderer = pdfRenderer;
    private readonly IInvoiceStorage _storage = storage;
    private readonly IFinanceUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<DownloadInvoiceQueryHandler> _logger = logger;

    public async Task<Result<DownloadInvoiceResult>> Handle(DownloadInvoiceQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<DownloadInvoiceResult>(
                new Error("Invoice.NotFound", "Invoice not found."),
                Outcome.NotFound);
        }

        var isOwner = invoice.UserId == request.CallerUserId;
        var isProvider = request.CallerProviderId is not null && invoice.ProviderId == request.CallerProviderId.Value;
        if (!request.CallerIsAdmin && !isOwner && !isProvider)
        {
            return Result.Failure<DownloadInvoiceResult>(
                new Error("Invoice.OwnerMismatch", "Caller is not authorized to download this invoice."),
                Outcome.Forbidden);
        }

        byte[]? pdfBytes = null;
        if (!string.IsNullOrWhiteSpace(invoice.PdfStoragePath))
        {
            pdfBytes = await _storage.ReadAsync(invoice.PdfStoragePath, cancellationToken);
        }

        if (pdfBytes is null)
        {
            // Lazy render — first download or stale cache
            _logger.LogDebug("Rendering invoice {InvoiceNumber} PDF on demand.", invoice.InvoiceNumber);
            pdfBytes = _pdfRenderer.Render(invoice);
            var path = await _storage.StoreAsync(invoice.UserId, invoice.Id, pdfBytes, cancellationToken);
            invoice.MarkPdfRendered(path);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new DownloadInvoiceResult(
            PdfBytes: pdfBytes,
            FileName: $"{invoice.InvoiceNumber}.pdf",
            ContentType: "application/pdf"));
    }
}
