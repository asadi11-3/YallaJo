namespace YallaJo.Web.Areas.Admin.Models.NotificationTemplates;

public sealed class CreateTemplateRequest
{
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
}

public sealed class UpdateTemplateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? RowVersion { get; set; }
}
