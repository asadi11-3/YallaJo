using MediatR;
using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetAdminReports;

/// <summary>Admin cursor-paginated list of submitted reports.</summary>
public sealed record GetAdminReportsQuery(
    Guid? AfterCursor = null,
    int PageSize = 20) : IRequest<Result<ReportPageDto>>;
