using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetMyInvoices;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetProviderInvoices;

public sealed record GetProviderInvoicesQuery(
    Guid ProviderId,
    Guid? Cursor,
    int PageSize) : IRequest<Result<InvoicePageDto>>;

public sealed class GetProviderInvoicesQueryHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetProviderInvoicesQuery, Result<InvoicePageDto>>
{
    private readonly IInvoiceRepository _invoiceRepository = invoiceRepository;

    public async Task<Result<InvoicePageDto>> Handle(GetProviderInvoicesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, next) = await _invoiceRepository.GetByProviderPageAsync(
            request.ProviderId,
            request.Cursor,
            pageSize,
            cancellationToken);

        var dtos = items.Select(GetMyInvoicesQueryHandler.MapToDto).ToList();
        return Result.Success(new InvoicePageDto(dtos, next));
    }
}
