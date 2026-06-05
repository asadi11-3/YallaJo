namespace YallaJo.Web.Areas.Admin.Models.Moderation;

public sealed class ModerationFilterRequest
{
    public Guid? AfterCursor { get; set; }
    public int PageSize { get; set; } = 20;
}

/// <summary>Outbound request body for POST /api/v1/social/moderation/warn.</summary>
public sealed record WarnUserApiRequest(Guid UserId, ModerationEntityType EntityType, Guid EntityId, string Reason);

/// <summary>Outbound request body for POST /api/v1/social/moderation/ban.</summary>
public sealed record BanUserApiRequest(Guid UserId, ModerationEntityType EntityType, Guid EntityId, string Reason, DateTime? ExpiresAt);
