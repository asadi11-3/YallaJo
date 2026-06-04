namespace YallaJo.Web.Areas.Admin.Models.NotificationTemplates;

public static class NotificationTemplatesMapper
{
    public static NotificationTemplatesListVm ToListVm(IReadOnlyList<NotificationTemplateResponse> templates)
    {
        return new NotificationTemplatesListVm
        {
            Templates = templates.Select(t => new NotificationTemplateRowVm
            {
                Id = t.Id,
                Type = t.Type,
                Channel = t.Channel,
                LanguageCode = t.LanguageCode,
                Title = t.Title,
                Body = t.Body,
                HtmlBody = t.HtmlBody,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                RowVersion = t.RowVersion,
            }).ToList(),
        };
    }

    public static NotificationTemplateEditVm ToEditVm(NotificationTemplateResponse t)
    {
        return new NotificationTemplateEditVm
        {
            Id = t.Id,
            Type = t.Type,
            Channel = t.Channel,
            LanguageCode = t.LanguageCode,
            Title = t.Title,
            Body = t.Body,
            HtmlBody = t.HtmlBody,
            RowVersion = t.RowVersion,
            IsNew = false,
        };
    }

    public static string ChannelColor(string channel) => channel switch
    {
        "Email" => "info",
        "Sms" => "warning",
        "Push" => "secondary",
        "InApp" => "secondary",
        _ => "secondary",
    };
}
