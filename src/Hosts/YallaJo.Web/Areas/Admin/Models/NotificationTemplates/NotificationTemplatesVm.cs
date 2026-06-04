namespace YallaJo.Web.Areas.Admin.Models.NotificationTemplates;

public sealed class NotificationTemplatesListVm
{
    public IReadOnlyList<NotificationTemplateRowVm> Templates { get; set; } = [];
}

public sealed class NotificationTemplateRowVm
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class NotificationTemplateEditVm
{
    public Guid? Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? RowVersion { get; set; }
    public bool IsNew { get; set; }
}
