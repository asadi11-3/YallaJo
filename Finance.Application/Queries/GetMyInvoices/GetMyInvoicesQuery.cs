using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetMyInvoices;

/// <summary>
/// Buyer-facing list — cursor pagination per F-R10.
/// </summary>
public sealed record GetMyInvoicesQuery(
    Guid UserId,
    Guid? Cursor,
    int PageSize) : IRequest<Result<InvoicePageDto>>;
