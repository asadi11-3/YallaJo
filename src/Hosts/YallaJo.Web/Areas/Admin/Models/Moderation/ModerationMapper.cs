namespace YallaJo.Web.Areas.Admin.Models.Moderation;

public static class ModerationMapper
{
    public static ModerationVm ToVm(ModerationLogPageResponse page)
    {
        return new ModerationVm
        {
            NextCursor = page.NextCursor,
            Logs = page.Items.Select(l => new ModerationLogRowVm
            {
                Id = l.Id,
                AdminUserId = l.AdminUserId,
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
}
