using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetMyInvoices;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(
    Guid InvoiceId,
    Guid CallerUserId,
    bool CallerIsAdmin,
    Guid? CallerProviderId) : IRequest<Result<InvoiceDto>>;

public sealed class GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoiceRepository = invoiceRepository;

    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<InvoiceDto>(
                new Error("Invoice.NotFound", "Invoice not found."),
                Outcome.NotFound);
        }

        var isOwner = invoice.UserId == request.CallerUserId;
        var isProvider = request.CallerProviderId is not null && invoice.ProviderId == request.CallerProviderId.Value;
        if (!request.CallerIsAdmin && !isOwner && !isProvider)
        {
            return Result.Failure<InvoiceDto>(
                new Error("Invoice.OwnerMismatch", "Caller is not authorized to read this invoice."),
                Outcome.Forbidden);
        }

        return Result.Success(GetMyInvoicesQueryHandler.MapToDto(invoice));
    }
}
