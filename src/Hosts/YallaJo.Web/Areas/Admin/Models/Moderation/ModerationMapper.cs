namespace YallaJo.Web.Areas.Admin.Models.Moderation;

public static class ModerationMapper
{
    public static ModerationVm ToVm(ModerationLogPageResponse page, IReadOnlyDictionary<Guid, string>? adminEmails = null)
    {
        return new ModerationVm
        {
            NextCursor = page.NextCursor,
            Logs = page.Items.Select(l => new ModerationLogRowVm
            {
                Id = l.Id,
                AdminUserId = l.AdminUserId,
                AdminEmail = adminEmails is not null && adminEmails.TryGetValue(l.AdminUserId, out var email) ? email : null,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Action = l.Action,
                Notes = l.Notes,
                ActionedAt = l.ActionedAt,
                SourceReportId = l.SourceReportId,
            }).ToList(),
        };
    }

    public static string ActionColor(string action) => action switch
    {
        _ when string.Equals(action, "BanUser", StringComparison.OrdinalIgnoreCase) => "danger",
        _ when string.Equals(action, "RemoveContent", StringComparison.OrdinalIgnoreCase) => "danger",
        _ when string.Equals(action, "WarnUser", StringComparison.OrdinalIgnoreCase) => "warning",
        _ when string.Equals(action, "UnbanUser", StringComparison.OrdinalIgnoreCase) => "success",
        _ when string.Equals(action, "RestoreContent", StringComparison.OrdinalIgnoreCase) => "success",
        _ when string.Equals(action, "Dismiss", StringComparison.OrdinalIgnoreCase) => "secondary",
        _ => "secondary",
    };

    public static string ActionIcon(string action) => action switch
    {
        _ when string.Equals(action, "BanUser", StringComparison.OrdinalIgnoreCase) => "ban",
        _ when string.Equals(action, "RemoveContent", StringComparison.OrdinalIgnoreCase) => "circle-xmark",
        _ when string.Equals(action, "WarnUser", StringComparison.OrdinalIgnoreCase) => "triangle-exclamation",
        _ when string.Equals(action, "UnbanUser", StringComparison.OrdinalIgnoreCase) => "circle-check",
        _ when string.Equals(action, "RestoreContent", StringComparison.OrdinalIgnoreCase) => "circle-check",
        _ when string.Equals(action, "Dismiss", StringComparison.OrdinalIgnoreCase) => "circle-info",
        _ => "circle-info",
    };
}
