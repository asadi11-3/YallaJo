using Finance.Application.Queries.Dtos;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetMyInvoices;

public sealed class GetMyInvoicesQueryHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetMyInvoicesQuery, Result<InvoicePageDto>>
{
    private readonly IInvoiceRepository _invoiceRepository = invoiceRepository;

    public async Task<Result<InvoicePageDto>> Handle(GetMyInvoicesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, next) = await _invoiceRepository.GetByUserPageAsync(
            request.UserId,
            request.Cursor,
            pageSize,
            cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return Result.Success(new InvoicePageDto(dtos, next));
    }

    internal static InvoiceDto MapToDto(Invoice invoice)
        => new(
            invoice.Id,
            invoice.PaymentId,
            invoice.BookingId,
            invoice.UserId,
            invoice.ProviderId,
            invoice.InvoiceNumber,
            invoice.Status.ToString(),
            invoice.Currency,
            invoice.AmountSubtotal.Amount,
            invoice.AmountTax.Amount,
            invoice.AmountDiscount.Amount,
            invoice.AmountTotal.Amount,
            invoice.IssuedAt,
            invoice.BuyerName,
            invoice.BuyerEmail,
            invoice.SellerName,
            invoice.SellerTaxId,
            invoice.PdfStoragePath is not null,
            invoice.Items.Select(i => new InvoiceItemDto(i.Description, i.Quantity, i.UnitPrice.Amount, i.Subtotal.Amount)).ToList());
}
