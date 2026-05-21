using MediatR;
using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetModerationLogs;

/// <summary>Admin cursor-paginated list of moderation log entries.</summary>
public sealed record GetModerationLogsQuery(
    Guid? AfterCursor = null,
    int PageSize = 20) : IRequest<Result<ModerationLogPageDto>>;
