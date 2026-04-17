
namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Responses
{
    public sealed class AuditLogItemResponse
    {
        public Guid Id { get; init; }
        public Guid? UserId { get; init; }
        public string Action { get; init; } = string.Empty;
        public string? IpAddress { get; init; }
        public DateTime OccurredAt { get; init; }
    }
}
