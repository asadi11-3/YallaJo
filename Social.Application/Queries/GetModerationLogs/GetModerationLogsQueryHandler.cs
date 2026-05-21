using MediatR;
using Social.Application.Queries.Dtos;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetModerationLogs;

internal sealed class GetModerationLogsQueryHandler(
    IContentModerationLogRepository logRepository)
    : IRequestHandler<GetModerationLogsQuery, Result<ModerationLogPageDto>>
{
    public async Task<Result<ModerationLogPageDto>> Handle(GetModerationLogsQuery request, CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var (items, nextCursor) = await logRepository.GetPageAsync(request.AfterCursor, pageSize, ct);

        var dtos = items.Select(MapToDto).ToList();
        return Result.Success(new ModerationLogPageDto(dtos, nextCursor));
    }

    private static ModerationLogDto MapToDto(ContentModerationLog l) => new(
        l.Id,
        l.AdminUserId,
        l.EntityType.ToString(),
        l.EntityId,
        l.Action.ToString(),
        l.Notes,
        l.ActionedAt,
        l.SourceReportId);
}
