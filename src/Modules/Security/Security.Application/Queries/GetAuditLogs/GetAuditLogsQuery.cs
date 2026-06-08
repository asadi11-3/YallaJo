using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery : IQuery<PagedAuditLogsResponse>
{
    public int Page { get; }
    public int PageSize { get; }
    public Guid? UserId { get; }
    public Guid? ActorUserId { get; }
    public string? Action { get; }
    public DateTime? From { get; }
    public DateTime? To { get; }

    public GetAuditLogsQuery(
        int page = 1,
        int pageSize = 20,
        Guid? userId = null,
        Guid? actorUserId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        Page = page < 1 ? 1 : page;
        PageSize = Math.Clamp(pageSize, 1, 50);
        UserId = userId;
        ActorUserId = actorUserId;
        Action = action;
        From = from;
        To = to;
    }
}
