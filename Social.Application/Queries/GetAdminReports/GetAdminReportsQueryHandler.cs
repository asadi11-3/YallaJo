using MediatR;
using Social.Application.Queries.Dtos;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetAdminReports;

internal sealed class GetAdminReportsQueryHandler(
    IReportRepository reportRepository)
    : IRequestHandler<GetAdminReportsQuery, Result<ReportPageDto>>
{
    public async Task<Result<ReportPageDto>> Handle(GetAdminReportsQuery request, CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var (items, nextCursor) = await reportRepository.GetAdminPageAsync(request.AfterCursor, pageSize, ct);

        var dtos = items.Select(MapToDto).ToList();
        return Result.Success(new ReportPageDto(dtos, nextCursor));
    }

    internal static ReportDto MapToDto(Report r) => new(
        r.Id,
        r.ReporterUserId,
        r.EntityType.ToString(),
        r.EntityId,
        r.Reason.ToString(),
        r.Description,
        r.Status.ToString(),
        r.SubmittedAt,
        r.ResolvedByUserId,
        r.ResolvedAt,
        r.ResolutionAction?.ToString(),
        r.ResolutionNotes);
}
